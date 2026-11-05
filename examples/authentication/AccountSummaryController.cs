using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Authentication;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Controllers;

/// <summary>Vzor chráněného controlleru pro navazující vývoj; do produkčního API se sám nenačítá.</summary>
/// <remarks>Zdroj kompiluje a přes HTTP ověřuje integrační testovací projekt.</remarks>
[ApiController]
[Authorize]
[Route("api/account/summary")]
public sealed class AccountSummaryController : ControllerBase
{
    /// <summary>Vrátí účet z ověřené session; identifikátor poslaný klientem nepoužívá.</summary>
    [HttpGet]
    public IActionResult Get()
    {
        // Identita pochází ze serverem ověřené session, nikdy z ID zaslaného klientem.
        var identity = HttpContext.Features.Get<SessionIdentity>()
            ?? throw new InvalidOperationException("Authenticated session is required.");
        return Ok(new { user = AuthUserDto.From(identity.User) });
    }

    /// <summary>Ukazuje omezení endpointu na vyučující; jiný přihlášený typ účtu dostane 403.</summary>
    [HttpGet("teaching")]
    [Authorize(Roles = "TEACHER")]
    public IActionResult GetTeaching() => Get();
}
