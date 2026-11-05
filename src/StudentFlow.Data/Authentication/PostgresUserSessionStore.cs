using Dapper;
using Npgsql;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Data.Authentication;

/// <summary>Spravuje session v PostgreSQL; zamknutí účtu při vytvoření chrání kontrolu hesla i rehash.</summary>
public sealed class PostgresUserSessionStore(NpgsqlDataSource dataSource) : IUserSessionStore
{
    private const string SelectSession = """
        SELECT u.user_id AS Id, u.display_name AS DisplayName, u.email AS Email,
               u.is_active AS IsActive, r.role_code AS AccountTypeCode,
               s.created_at AS CreatedAt, s.expires_at AS ExpiresAt, s.persistent AS Persistent
        FROM user_session s JOIN app_user u USING (user_id)
        JOIN user_role ur USING (user_id) JOIN app_role r USING (role_id)
        WHERE r.role_code IN ('STUDENT', 'TEACHER')
        """;

    /// <inheritdoc />
    public async Task<SessionIdentity?> CreateAsync(long userId, string expectedPasswordHash, string? replacementPasswordHash,
        byte[] tokenHash, bool persistent, byte[]? previousTokenHash, DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ValidateHash(tokenHash);
        if (previousTokenHash is not null) ValidateHash(previousTokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPasswordHash);
        if (replacementPasswordHash is not null) ArgumentException.ThrowIfNullOrWhiteSpace(replacementPasswordHash);
        now = now.ToUniversalTime();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var owner = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition("""
            SELECT user_id FROM app_user
            WHERE user_id = @UserId AND password_hash = @ExpectedPasswordHash AND is_active
            FOR UPDATE
            """, new { UserId = userId, ExpectedPasswordHash = expectedPasswordHash }, transaction, cancellationToken: cancellationToken));
        if (owner is null) return null;

        if (replacementPasswordHash is not null)
            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE app_user SET password_hash = @Hash, updated_at = @Now WHERE user_id = @UserId
                """, new { UserId = userId, Hash = replacementPasswordHash, Now = now }, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO user_session (user_id, token_hash, created_at, expires_at, persistent)
            VALUES (@UserId, @TokenHash, @Now,
                    @Now + CASE WHEN @Persistent THEN interval '720 hours' ELSE interval '8 hours' END, @Persistent)
            """, new { UserId = userId, TokenHash = tokenHash, Now = now, Persistent = persistent }, transaction, cancellationToken: cancellationToken));
        if (previousTokenHash is not null)
            await connection.ExecuteAsync(new CommandDefinition("""
                UPDATE user_session SET revoked_at = GREATEST(@Now, created_at)
                WHERE token_hash = @Hash AND revoked_at IS NULL AND expires_at > @Now
                """, new { Hash = previousTokenHash, Now = now }, transaction, cancellationToken: cancellationToken));

        var row = await connection.QuerySingleAsync<SessionRow>(new CommandDefinition(
            SelectSession + " AND s.token_hash = @Hash", new { Hash = tokenHash }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return row.ToIdentity();
    }

    /// <inheritdoc />
    public async Task<SessionIdentity?> FindActiveAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ValidateHash(tokenHash);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<SessionRow>(new CommandDefinition(
            SelectSession + " AND s.token_hash = @Hash AND s.revoked_at IS NULL AND s.expires_at > @Now AND u.is_active",
            new { Hash = tokenHash, Now = now.ToUniversalTime() }, cancellationToken: cancellationToken));
        return row?.ToIdentity();
    }

    /// <inheritdoc />
    public async Task RevokeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        ValidateHash(tokenHash);
        await using var command = dataSource.CreateCommand("""
            UPDATE user_session SET revoked_at = GREATEST($1, created_at)
            WHERE token_hash = $2 AND revoked_at IS NULL
            """);
        command.Parameters.AddWithValue(now.ToUniversalTime());
        command.Parameters.AddWithValue(tokenHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void ValidateHash(byte[] value)
    {
        if (value is not { Length: 32 }) throw new ArgumentException("A session token hash must contain 32 bytes.");
    }

    private sealed class SessionRow
    {
        public long Id { get; set; }
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool IsActive { get; set; }
        public string AccountTypeCode { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Persistent { get; set; }
        public SessionIdentity ToIdentity() => new(new(Id, DisplayName, Email, AccountTypeCodes.FromCode(AccountTypeCode), IsActive),
            new(new DateTimeOffset(CreatedAt), new DateTimeOffset(ExpiresAt), Persistent));
    }
}
