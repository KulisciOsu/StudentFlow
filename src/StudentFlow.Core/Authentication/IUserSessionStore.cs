namespace StudentFlow.Core.Authentication;

/// <summary>Časové hranice session; používáním se její platnost neprodlužuje.</summary>
/// <param name="CreatedAt">Okamžik vytvoření session.</param>
/// <param name="ExpiresAt">Pevný konec platnosti: 8 hodin, nebo 30 dní od vytvoření.</param>
/// <param name="Persistent">Zda má session přetrvat zavření prohlížeče pomocí trvalé cookie.</param>
public sealed record SessionInfo(DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, bool Persistent);
/// <summary>Aktuální identita účtu spolu s údaji o jeho ověřené serverové session.</summary>
public sealed record SessionIdentity(UserIdentity User, SessionInfo Session);

/// <summary>Správa serverových session pomocí SHA-256 hashů tokenů; syrové tokeny se neukládají.</summary>
public interface IUserSessionStore
{
    /// <summary>Atomicky ověří aktuální heslo a aktivitu účtu, případně provede rehash a založí session.</summary>
    /// <remarks>Je-li předán hash předchozího tokenu, zneplatní také tuto session v téže transakci.</remarks>
    /// <returns>Novou session, nebo null, pokud se heslo změnilo či účet již není aktivní.</returns>
    Task<SessionIdentity?> CreateAsync(long userId, string expectedPasswordHash, string? replacementPasswordHash,
        byte[] tokenHash, bool persistent, byte[]? previousTokenHash, DateTimeOffset now,
        CancellationToken cancellationToken = default);
    /// <summary>Vrátí nezneplatněnou session aktivního účtu, jejíž expirace je později než now; jinak null.</summary>
    Task<SessionIdentity?> FindActiveAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
    /// <summary>Zneplatní session podle hashe; neexistující či již zneplatněný token je bez účinku.</summary>
    Task RevokeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default);
}
