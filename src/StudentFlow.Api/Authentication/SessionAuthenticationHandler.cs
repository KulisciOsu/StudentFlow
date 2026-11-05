using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

/// <summary>Ověřuje cookie proti serverovým session a převádí aktuální účet na přihlášeného uživatele.</summary>
public sealed class SessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IUserSessionStore sessions, TimeProvider clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Načte platnou session a uloží její identitu do claims i HttpContext.Features pro controllery.</summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hash = SessionToken.Hash(Request.Cookies[SessionToken.CookieName]);
        if (hash is null) return AuthenticateResult.NoResult();
        var session = await sessions.FindActiveAsync(hash, clock.GetUtcNow(), Context.RequestAborted);
        if (session is null) return AuthenticateResult.Fail("Invalid session.");
        Context.Features.Set(session);
        var user = session.User;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.DisplayName), new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, AccountTypeCodes.ToCode(user.AccountType))
        };
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, SessionToken.Scheme)), SessionToken.Scheme));
    }

    /// <summary>Vrátí API chybu 401 bez přesměrování na přihlašovací stránku.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return ApiProblems.WriteAsync(Context, 401, "UNAUTHENTICATED", "Přihlášení je vyžadováno", "Přihlaste se znovu.");
    }

    /// <summary>Vrátí API chybu 403, pokud přihlášenému účtu chybí požadované oprávnění.</summary>
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        ApiProblems.WriteAsync(Context, 403, "FORBIDDEN", "Přístup odepřen", "K tomuto obsahu nemáte přístup.");
}
