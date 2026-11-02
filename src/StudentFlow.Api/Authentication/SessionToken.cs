using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace StudentFlow.Api.Authentication;

public static class SessionToken
{
    public const string CookieName = "__Host-StudentFlow.Session";
    public const string Scheme = "StudentFlowSession";
    public static string Generate() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    public static byte[]? Hash(string? token)
    {
        if (token is not { Length: 43 } || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))) return null;
        var bytes = WebEncoders.Base64UrlDecode(token);
        return bytes.Length == 32 && WebEncoders.Base64UrlEncode(bytes) == token ? SHA256.HashData(bytes) : null;
    }
    public static CookieOptions CookieOptions(DateTimeOffset? expiresAt = null) => new()
    {
        Secure = true, HttpOnly = true, SameSite = SameSiteMode.Lax, Path = "/",
        Expires = expiresAt, MaxAge = expiresAt is null ? null : TimeSpan.FromDays(30)
    };
    public static void Delete(HttpResponse response) => response.Cookies.Delete(CookieName, CookieOptions());
}
