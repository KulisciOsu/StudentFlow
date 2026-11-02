using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentFlow.Api.Authentication;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAntiforgery antiforgery, IUserAccountStore accounts, IPasswordHasher<object> hasher) : ControllerBase
{
    [HttpGet("csrf")]
    [AllowAnonymous]
    public IActionResult Csrf()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken, headerName = "X-CSRF-TOKEN" });
    }

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

}
