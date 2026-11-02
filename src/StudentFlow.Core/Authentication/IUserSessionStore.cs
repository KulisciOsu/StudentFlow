namespace StudentFlow.Core.Authentication;

public sealed record SessionInfo(DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool Persistent);
public sealed record SessionIdentity(UserIdentity User, SessionInfo Session);

public interface IUserSessionStore
{
    // Kontrola aktuálního hesla/aktivity, případný rehash, vložení a rotace jsou atomické.
    Task<SessionIdentity?> CreateAsync(long userId, string expectedPasswordHash, string? replacementPasswordHash,
        byte[] tokenHash, bool persistent, byte[]? previousTokenHash, DateTimeOffset now,
        CancellationToken cancellationToken = default);
    Task<SessionIdentity?> FindActiveAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task RevokeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
}
