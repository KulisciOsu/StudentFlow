using Dapper;
using Npgsql;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Data.Authentication;

/// <summary>Ukládá účty v PostgreSQL; normalizaci e-mailu a unikátnost zajišťují databázová pravidla.</summary>
public sealed class PostgresUserAccountStore(NpgsqlDataSource dataSource) : IUserAccountStore
{
    private const string SelectUser = """
        SELECT u.user_id AS Id, u.display_name AS DisplayName, u.email AS Email,
               u.password_hash AS PasswordHash, u.is_active AS IsActive,
               r.role_code AS AccountTypeCode
        FROM app_user u
        JOIN user_role ur ON ur.user_id = u.user_id
        JOIN app_role r ON r.role_id = ur.role_id
        WHERE r.role_code IN ('STUDENT', 'TEACHER')
        """;

    /// <inheritdoc />
    public async Task<CreateUserResult> CreateAsync(NewUserAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.DisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(account.PasswordHash);
        var code = AccountTypeCodes.ToCode(account.AccountType);
        var name = account.DisplayName.Trim();
        var email = account.Email.Trim();
        if (name.EnumerateRunes().Count() > 160 || email.EnumerateRunes().Count() is < 3 or > 320)
            throw new ArgumentException("Account fields exceed their allowed lengths.", nameof(account));

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var inserted = await connection.QuerySingleAsync<InsertedUser>(new CommandDefinition("""
                INSERT INTO app_user (display_name, email, password_hash)
                VALUES (@Name, normalize_account_email(@Email), @Hash)
                RETURNING user_id AS Id, email AS Email
                """, new { Name = name, Email = email, Hash = account.PasswordHash }, transaction, cancellationToken: cancellationToken));
            var assigned = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO user_role (user_id, role_id)
                SELECT @Id, role_id FROM app_role WHERE role_code = @Code
                """, new { Id = inserted.Id, Code = code }, transaction, cancellationToken: cancellationToken));
            if (assigned != 1)
                throw new InvalidOperationException("The required account type role is missing.");

            await transaction.CommitAsync(cancellationToken);
            return new(CreateUserStatus.Created, new(inserted.Id, name, inserted.Email, account.AccountType, true));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation
            && exception.ConstraintName is "app_user_email_key" or "app_user_normalized_email_key")
        {
            return new(CreateUserStatus.EmailAlreadyRegistered, null);
        }
    }

    /// <inheritdoc />
    public async Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            SelectUser + " AND u.normalized_email = normalize_account_email(@Email)", new { Email = email.Trim() },
            cancellationToken: cancellationToken));
        return row is null ? null : new(row.ToIdentity(), row.PasswordHash);
    }

    /// <inheritdoc />
    public async Task<UserIdentity?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            SelectUser + " AND u.user_id = @Id", new { Id = id }, cancellationToken: cancellationToken));
        return row?.ToIdentity();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT r.role_code FROM user_role ur JOIN app_role r USING (role_id)
            WHERE ur.user_id = @Id ORDER BY r.role_code
            """, new { Id = id }, cancellationToken: cancellationToken));
        return roles.AsList();
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePasswordHashAsync(long id, string expectedHash, string newHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var changed = await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE app_user SET password_hash = @NewHash, updated_at = now()
            WHERE user_id = @Id AND password_hash = @ExpectedHash
            """, new { Id = id, ExpectedHash = expectedHash, NewHash = newHash }, cancellationToken: cancellationToken));
        return changed == 1;
    }

    private sealed class InsertedUser
    {
        public long Id { get; set; }
        public string Email { get; set; } = "";
    }

    private sealed class UserRow
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public bool IsActive { get; set; }
        public string AccountTypeCode { get; set; } = "";
        public UserIdentity ToIdentity() => new(Id, DisplayName, Email, AccountTypeCodes.FromCode(AccountTypeCode), IsActive);
    }
}
