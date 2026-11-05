using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentFlow.Api.Authentication;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Controllers;

/// <summary>HTTP rozhraní registrace, přihlášení a správy identity pomocí serverové session.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAntiforgery antiforgery, IUserAccountStore accounts, IPasswordHasher<object> hasher,
    IUserSessionStore sessions, LoginPasswordVerifier passwordVerifier, TimeProvider clock) : ControllerBase
{
    /// <summary>Vydá CSRF token pro aktuální identitu a nastaví odpovídající ochrannou cookie.</summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }

    /// <summary>Založí účet s jedním typem a vrátí jeho veřejné údaje; přihlášení tím nevzniká.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [RequestSizeLimit(AuthErrorMiddleware.MaxAuthBytes)]
    [EnableRateLimiting("auth-register")]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    [ServiceFilter(typeof(AuthBodyLimitFilter))]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
            return ApiProblems.Result(HttpContext, 400, "VALIDATION_FAILED", "Neplatné vstupní údaje", "Opravte označená pole.", errors);

        var hash = hasher.HashPassword(request, request.Password!);
        var result = await accounts.CreateAsync(new NewUserAccount(request.DisplayName!, request.Email!, hash, request.GetAccountType()), cancellationToken);
        if (result.Status == CreateUserStatus.EmailAlreadyRegistered)
            return ApiProblems.Result(HttpContext, 409, "EMAIL_ALREADY_REGISTERED", "Účet již existuje",
                "Účet s tímto e-mailem už existuje. Přihlaste se.", new Dictionary<string, string[]>
                {
                    ["email"] = ["Účet s tímto e-mailem už existuje."]
                });
        return StatusCode(201, new { user = AuthUserDto.From(result.User!) });
    }

    /// <summary>Ověří heslo a nastaví cookie nové session; předchozí session tohoto prohlížeče zneplatní.</summary>
    /// <remarks>Neexistující, neaktivní účet i chybné heslo vracejí stejné odmítnutí.</remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [RequestSizeLimit(AuthErrorMiddleware.MaxAuthBytes)]
    [EnableRateLimiting("auth-login")]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    [ServiceFilter(typeof(AuthBodyLimitFilter))]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
            return ApiProblems.Result(HttpContext, 400, "VALIDATION_FAILED", "Neplatné vstupní údaje", "Opravte označená pole.", errors);
        var credentials = await accounts.FindByEmailAsync(request.Email!, cancellationToken);
        var verification = passwordVerifier.Verify(credentials?.PasswordHash, request.Password!);
        if (credentials is null || !credentials.User.IsActive || verification == PasswordVerificationResult.Failed) return InvalidCredentials();
        var replacement = verification == PasswordVerificationResult.SuccessRehashNeeded ? hasher.HashPassword(request, request.Password!) : null;
        var token = SessionToken.Generate();
        var session = await sessions.CreateAsync(credentials.User.Id, credentials.PasswordHash, replacement,
            SessionToken.Hash(token)!, request.RememberMe, SessionToken.Hash(Request.Cookies[SessionToken.CookieName]), clock.GetUtcNow(), cancellationToken);
        if (session is null) return InvalidCredentials();
        Response.Cookies.Append(SessionToken.CookieName, token, SessionToken.CookieOptions(session.Session.Persistent ? session.Session.ExpiresAt : null));
        return Ok(ToResponse(session));
    }

    /// <summary>Vrátí serverem ověřenou identitu a pevný konec platnosti aktuální session.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(ToResponse(HttpContext.Features.Get<SessionIdentity>()!));

    /// <summary>Zneplatní session a odstraní cookie; opakované odhlášení bez platné session je přípustné.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ServiceFilter(typeof(CsrfValidationFilter))]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var hash = SessionToken.Hash(Request.Cookies[SessionToken.CookieName]);
        if (hash is not null) await sessions.RevokeAsync(hash, clock.GetUtcNow(), cancellationToken);
        SessionToken.Delete(Response);
        return NoContent();
    }

    private ObjectResult InvalidCredentials() => ApiProblems.Result(HttpContext, 401, "INVALID_CREDENTIALS",
        "Přihlášení se nezdařilo", "E-mail nebo heslo nejsou správné.");
    private static object ToResponse(SessionIdentity identity) => new
    {
        user = AuthUserDto.From(identity.User),
        session = new { identity.Session.ExpiresAt, identity.Session.Persistent }
    };
}
