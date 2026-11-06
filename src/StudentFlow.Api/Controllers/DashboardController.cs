
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Models;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API pro úvodní dashboard přihlášeného uživatele.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    /// <summary>
    /// Vrací přehled projektů a úkolů pro dashboard.
    /// Zatím používá ukázková data bez databázového napojení.
    /// </summary>
    /// <returns>Data úvodního dashboardu.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<DashboardDto> Get()
    {
        var dashboard = new DashboardDto
        {
            Projects =
            [
                new DashboardProjectDto
                {
                    Id = 1,
                    Name = "Rezervační systém knihovny",
                    Team = "Tým Sova",
                    MembersCount = 5,
                    Role = "Vedoucí",
                    Completed = 3,
                    Total = 10,
                    OpenTasks = 1,
                    OverdueTasks = 0,
                    Milestone = "Inception, 31. 10.",
                    Members = ["PS", "JN", "TD", "LČ", "MK"]
                }
            ],
            Tasks =
            [
                new DashboardTaskDto
                {
                    Id = 1,
                    Name = "Wireframy obrazovek",
                    Project = "Rezervační systém knihovny",
                    Deadline = "30. 10.",
                    Priority = "Střední",
                    Status = "Rozpracováno",
                    Overdue = false,
                    Completed = false
                }
            ]
        };

        return Ok(dashboard);
    }
}
