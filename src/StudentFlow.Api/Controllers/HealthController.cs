using Microsoft.AspNetCore.Mvc;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// Poskytuje kontrolu dostupnosti API.
/// </summary>
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Vrací stav potvrzující, že API přijímá požadavky.
    /// </summary>
    /// <returns>HTTP 200 s odpovědí obsahující stav OK.</returns>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "OK" });
    }
}
