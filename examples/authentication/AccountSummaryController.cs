using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Authentication;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Controllers;

// Vzor pro nový controller. Do produkčního API se sám nenačítá;
// zdroj kompiluje a přes HTTP ověřuje integrační testovací projekt.
[ApiController]
[Authorize]
[Route("api/account/summary")]
public sealed class AccountSummaryController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        // Identita pochází ze serverem ověřené session, nikdy z ID zaslaného klientem.
        var identity = HttpContext.Features.Get<SessionIdentity>()
            ?? throw new InvalidOperationException("Authenticated session is required.");
        return Ok(new { user = AuthUserDto.From(identity.User) });
    }

    [HttpGet("teaching")]
    [Authorize(Roles = "TEACHER")]
    public IActionResult GetTeaching() => Get();
}
