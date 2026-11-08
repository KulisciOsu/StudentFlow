
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Models;
using StudentFlow.Core.Dashboard;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API pro dashboard přihlášeného uživatele.
/// </summary>
[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(
    IDashboardRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardDto>> Get(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!long.TryParse(
            userIdValue,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var userId))
        {
            return Unauthorized();
        }

        var data = await repository.GetDashboardAsync(
            userId, cancellationToken);

        return Ok(new DashboardDto
        {
            Projects = data.Projects.Select(p => new DashboardProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Team = p.Team,
                MembersCount = p.MembersCount,
                Role = p.Role,
                Completed = p.Completed,
                Total = p.Total,
                OpenTasks = p.OpenTasks,
                OverdueTasks = p.OverdueTasks,
                Milestone = p.Milestone,
                Members = p.Members
            }).ToList(),

            Tasks = data.Tasks.Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                Name = t.Name,
                Project = t.Project,
                Deadline = t.Deadline,
                Priority = t.Priority,
                Status = t.Status,
                Overdue = t.Overdue,
                Completed = t.Completed
            }).ToList()
        });
    }
}
