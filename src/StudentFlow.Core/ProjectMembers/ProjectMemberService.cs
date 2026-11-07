using System.Net.Mail;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Core.ProjectMembers;

/// <summary>Pravidla zobrazení, přidání a odebrání členů projektu.</summary>
/// <remarks>
/// Členství i vedení volajícího se ověřuje při každém volání z úložiště, nikdy z údajů klienta.
/// Nečlen dostává stejný výsledek jako u neexistujícího projektu, aby se cizí projekty neprozradily.
/// </remarks>
public sealed class ProjectMemberService(IProjectMemberStore store)
{
    /// <summary>Vrátí členy projektu, pokud je volající jeho aktuálním členem; vedoucí i běžný člen mají přístup.</summary>
    public async Task<ListMembersResult> ListAsync(int projectId, long callerUserId, CancellationToken cancellationToken = default)
    {
        if (await store.FindMemberAsync(projectId, callerUserId, cancellationToken) is null)
            return new(ListMembersStatus.ProjectNotFound, []);
        return new(ListMembersStatus.Listed, await store.ListAsync(projectId, cancellationToken));
    }

    /// <summary>Přidá aktivní účet typu STUDENT podle e-mailu jako běžného člena; smí pouze vedoucí projektu.</summary>
    /// <remarks>Pořadí kontrol: formát e-mailu, členství a vedení volajícího, existence a typ účtu, duplicita.</remarks>
    public async Task<AddMemberResult> AddByEmailAsync(int projectId, long callerUserId, string? email,
        CancellationToken cancellationToken = default)
    {
        var trimmedEmail = email?.Trim() ?? "";
        if (!IsValidEmail(trimmedEmail)) return new(AddMemberStatus.InvalidEmail, null);

        var caller = await store.FindMemberAsync(projectId, callerUserId, cancellationToken);
        if (caller is null) return new(AddMemberStatus.ProjectNotFound, null);
        if (!caller.IsLeader) return new(AddMemberStatus.NotLeader, null);

        var account = await store.FindAccountByEmailAsync(trimmedEmail, cancellationToken);
        if (account is null || !account.IsActive) return new(AddMemberStatus.AccountNotFound, null);
        if (account.AccountType != AccountType.Member) return new(AddMemberStatus.AccountNotStudent, null);

        // Úložiště při zápisu znovu ověří vedoucího a existující členství vrátí jako AlreadyMember.
        return await store.AddAsync(projectId, account.Id, callerUserId, cancellationToken);
    }

    /// <summary>Odebere běžného člena; smí pouze vedoucí projektu a vedoucího nelze odebrat.</summary>
    /// <remarks>Úkoly, komentáře a další příspěvky odebraného člena zůstávají beze změny.</remarks>
    public async Task<RemoveMemberStatus> RemoveAsync(int projectId, long callerUserId, long memberUserId,
        CancellationToken cancellationToken = default)
    {
        var caller = await store.FindMemberAsync(projectId, callerUserId, cancellationToken);
        if (caller is null) return RemoveMemberStatus.ProjectNotFound;
        if (!caller.IsLeader) return RemoveMemberStatus.NotLeader;

        var member = await store.FindMemberAsync(projectId, memberUserId, cancellationToken);
        if (member is null) return RemoveMemberStatus.MemberNotFound;
        if (member.IsLeader) return RemoveMemberStatus.LeaderCannotBeRemoved;

        // Souběžné odebrání téhož člena se projeví jako nenalezené členství.
        return await store.RemoveAsync(projectId, memberUserId, callerUserId, cancellationToken)
            ? RemoveMemberStatus.Removed
            : RemoveMemberStatus.MemberNotFound;
    }

    /// <summary>Stejná pravidla formátu jako registrace: 3 až 320 znaků, bez mezer a s právě jedním zavináčem.</summary>
    private static bool IsValidEmail(string email) =>
        email.EnumerateRunes().Count() is >= 3 and <= 320
        && !email.Any(char.IsWhiteSpace)
        && email.Count(c => c == '@') == 1
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email;
}
