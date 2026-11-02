using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace StudentFlow.Api.Authentication;

// I neexistující účet provede drahé ověření; nepoužívá se rychlá větev podle existence e-mailu.
public sealed class LoginPasswordVerifier
{
    private readonly PasswordHasher<object> hasher;
    private readonly string dummyHash;
    public LoginPasswordVerifier(IOptions<PasswordHasherOptions> options)
    {
        hasher = new PasswordHasher<object>(options);
        dummyHash = hasher.HashPassword(this, SessionToken.Generate());
    }
    public PasswordVerificationResult Verify(string? hash, string password) => hasher.VerifyHashedPassword(this, hash ?? dummyHash, password);
}
