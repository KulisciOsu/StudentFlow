using Microsoft.AspNetCore.Mvc;
using StudentFlow.Core.Kanban;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API sloupců nástěnky.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:int}/columns")]
public class ColumnsController : ControllerBase
{
    private readonly IKanbanBoardRepository _repository;

    /// <summary>
    /// Vytvoří controller sloupců.
    /// </summary>
    /// <param name="repository">Data nástěnky.</param>
    public ColumnsController(IKanbanBoardRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Vrací sloupce nástěnky projektu.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Seznam sloupců, 404 pokud projekt neexistuje, nebo 403 pokud k němu uživatel nemá přístup.</returns>
    [HttpGet]
    public async Task<ActionResult<List<KanbanBoardColumnDto>>> Get(int projectId)
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

        return await _repository.GetColumnsAsync(projectId);
    }
}
