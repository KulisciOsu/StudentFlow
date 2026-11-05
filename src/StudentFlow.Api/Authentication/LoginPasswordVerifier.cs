using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace StudentFlow.Api.Authentication;

/// <summary>Provádí drahé ověření hesla i pro neexistující účet, aby se omezilo rozlišování podle času.</summary>
public sealed class LoginPasswordVerifier
{
    private readonly PasswordHasher<object> hasher;
    private readonly string dummyHash;
    public LoginPasswordVerifier(IOptions<PasswordHasherOptions> options)
    {
        hasher = new PasswordHasher<object>(options);
        dummyHash = hasher.HashPassword(this, SessionToken.Generate());
    }
    /// <summary>Ověří předaný hash, nebo při jeho absenci zástupný hash se stejnou konfigurací.</summary>
    /// <remarks>Výsledek nenahrazuje kontrolu existence a aktivity účtu v přihlašovacím endpointu.</remarks>
    public PasswordVerificationResult Verify(string? hash, string password) => hasher.VerifyHashedPassword(this, hash ?? dummyHash, password);
}
