using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.DTOs;
using StudentFlow.Api.Services;

namespace StudentFlow.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(ProjectService projectService) : ControllerBase
{
    // TODO: vzít z přihlášeného uživatele, až bude hotové přihlášení.
    private const int FounderUserId = 1;

    [HttpPost]
    public async Task<ActionResult<CreatedProjectDto>> Create(
        CreateProjectRequestDto request, CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request, FounderUserId, cancellationToken);

        return Created($"/api/projects/{project.Id}", new CreatedProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            TeamId = project.Team.Id,
        });
    }
}
