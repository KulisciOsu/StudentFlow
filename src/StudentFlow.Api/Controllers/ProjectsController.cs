using Microsoft.AspNetCore.Mvc;
using StudentFlow.Core.Kanban;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API projektů nástěnky.
/// </summary>
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IKanbanBoardRepository _repository;

    /// <summary>
    /// Vytvoří controller projektů.
    /// </summary>
    /// <param name="repository">Data nástěnky.</param>
    public ProjectsController(IKanbanBoardRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Vrací projekt nástěnky.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Projekt, 404 pokud neexistuje, nebo 403 pokud k němu uživatel nemá přístup.</returns>
    [HttpGet("{projectId:int}")]
    public async Task<ActionResult<KanbanProjectDto>> Get(int projectId)
    {
        var project = await _repository.GetProjectAsync(projectId);

        if (project is null)
        {
            return NotFound();
        }

        var role = await BoardAccess.GetRoleAsync(_repository, User, projectId);

        if (!BoardPermissions.CanView(role))
        {
            return Forbid();
        }

        project.IsReadOnly = BoardPermissions.IsReadOnly(role);

        return project;
    }
}
