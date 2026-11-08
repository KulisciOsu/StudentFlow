using StudentFlow.Core.Authentication;
using StudentFlow.Core.ProjectMembers;

namespace StudentFlow.Core.Tests.ProjectMembers;

/// <summary>
/// Úložiště členů v paměti pro jednotkové testy pravidel; napodobuje podmíněné zápisy PostgreSQL úložiště.
/// </summary>
internal sealed class InMemoryProjectMemberStore : IProjectMemberStore
{
    private static readonly DateTimeOffset AddedAt = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
    private readonly List<UserIdentity> accounts = [];
    private readonly Dictionary<(int ProjectId, long UserId), ProjectMember> members = [];

    /// <summary>
    /// Počet volání rozhraní úložiště; příprava testovacích dat se nezapočítává.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    /// Kdo vytvořil dané členství; odpovídá sloupci added_by.
    /// </summary>
    public Dictionary<(int ProjectId, long UserId), long> AddedBy { get; } = [];

    /// <summary>
    /// Při odebrání nejprve smaže členství, jako by předběhl souběžný požadavek, a vrátí false.
    /// </summary>
    public bool SimulateConcurrentRemoval { get; set; }

    /// <summary>
    /// Připraví účet s daným typem a aktivitou.
    /// </summary>
    public void AddAccount(long id, string displayName, string email, AccountType type, bool isActive = true) =>
        accounts.Add(new(id, displayName, email, type, isActive));

    /// <summary>
    /// Připraví členství existujícího účtu v projektu.
    /// </summary>
    public void AddMembership(int projectId, long userId, bool isLeader)
    {
        var account = accounts.Single(a => a.Id == userId);
        members[(projectId, userId)] = new(userId, account.DisplayName, account.Email, isLeader, AddedAt);
    }

    /// <summary>
    /// Zjistí členství přímo z dat, bez započtení volání.
    /// </summary>
    public bool IsMember(int projectId, long userId) => members.ContainsKey((projectId, userId));

    /// <summary>
    /// Zjistí, zda je uživatel vedoucím projektu, bez započtení volání.
    /// </summary>
    public bool IsLeader(int projectId, long userId) =>
        members.TryGetValue((projectId, userId), out var member) && member.IsLeader;

    /// <summary>
    /// Počet členů projektu bez započtení volání.
    /// </summary>
    public int CountMembers(int projectId) => members.Keys.Count(key => key.ProjectId == projectId);

    /// <inheritdoc />
    public Task<ProjectMember?> FindMemberAsync(int projectId, long userId, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(members.GetValueOrDefault((projectId, userId)));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ProjectMember>> ListAsync(int projectId, CancellationToken cancellationToken = default)
    {
        CallCount++;
        IReadOnlyList<ProjectMember> list = members
            .Where(pair => pair.Key.ProjectId == projectId)
            .Select(pair => pair.Value)
            .OrderByDescending(member => member.IsLeader)
            .ThenBy(member => member.DisplayName, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<UserIdentity?> FindAccountByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        CallCount++;
        // Zjednodušená náhrada normalize_account_email: okolní mezery a velikost písmen.
        return Task.FromResult(accounts.FirstOrDefault(account =>
            string.Equals(account.Email, email.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    /// <inheritdoc />
    public Task<AddMemberResult> AddAsync(int projectId, long userId, long addedByUserId, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (!IsLeader(projectId, addedByUserId))
            return Task.FromResult(new AddMemberResult(AddMemberStatus.NotLeader, null));
        if (IsMember(projectId, userId))
            return Task.FromResult(new AddMemberResult(AddMemberStatus.AlreadyMember, null));

        AddMembership(projectId, userId, isLeader: false);
        AddedBy[(projectId, userId)] = addedByUserId;
        return Task.FromResult(new AddMemberResult(AddMemberStatus.Added, members[(projectId, userId)]));
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(int projectId, long userId, long removedByUserId, CancellationToken cancellationToken = default)
    {
        CallCount++;
        if (SimulateConcurrentRemoval)
        {
            members.Remove((projectId, userId));
            return Task.FromResult(false);
        }

        var removable = IsLeader(projectId, removedByUserId)
            && members.TryGetValue((projectId, userId), out var member)
            && !member.IsLeader;
        return Task.FromResult(removable && members.Remove((projectId, userId)));
    }
}
