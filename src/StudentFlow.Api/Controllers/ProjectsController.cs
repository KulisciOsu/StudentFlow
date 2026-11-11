
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Core.Kanban;
using StudentFlow.Core.Projects;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API projektů nástěnky.
/// </summary>
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IKanbanBoardRepository _kanbanRepository;
    private readonly IProjectCreationRepository _projectCreationRepository;

    /// <summary>
    /// Vytvoří controller projektů.
    /// </summary>
    /// <param name="kanbanRepository">Repozitář pro načítání projektů nástěnky.</param>
    /// <param name="projectCreationRepository">Repozitář pro vytváření projektů.</param>
    public ProjectsController(
        IKanbanBoardRepository kanbanRepository,
        IProjectCreationRepository projectCreationRepository)
    {
        _kanbanRepository = kanbanRepository;
        _projectCreationRepository = projectCreationRepository;
    }

    /// <summary>
    /// Vrací projekt nástěnky.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Projekt nástěnky, nebo odpověď 404, pokud projekt neexistuje.</returns>
    [HttpGet("{projectId:int}")]
    public async Task<ActionResult<KanbanProjectDto>> Get(int projectId)
    {
        var project = await _kanbanRepository.GetProjectAsync(projectId);

        if (project is null)
        {
            return NotFound();
        }

        return project;
    }

    /// <summary>
    /// Založí nový projekt a nastaví přihlášeného uživatele jako vedoucího.
    /// </summary>
    /// <param name="command">Údaje potřebné k vytvoření projektu.</param>
    /// <param name="cancellationToken">Token pro zrušení požadavku.</param>
    /// <returns>Vytvořený projekt, nebo validační či autorizační chybu.</returns>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CreatedProject>> Create(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null)
        {
            ModelState.AddModelError(
                nameof(command),
                "Tělo požadavku je povinné.");

            return ValidationProblem(ModelState);
        }

        if (command.Name is null ||
            string.IsNullOrWhiteSpace(command.Name) ||
            command.Name.Length > 200)
        {
            ModelState.AddModelError(
                nameof(command.Name),
                "Název projektu je povinný a může mít maximálně 200 znaků.");
        }

        if (command.Description is null || command.Description.Length > 1000)
        {
            ModelState.AddModelError(
                nameof(command.Description),
                "Popis projektu může mít maximálně 1 000 znaků.");
        }

        if (command.TeamName is null ||
            string.IsNullOrWhiteSpace(command.TeamName) ||
            command.TeamName.Length > 120)
        {
            ModelState.AddModelError(
                nameof(command.TeamName),
                "Název týmu je povinný a může mít maximálně 120 znaků.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(
                userIdValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var creatorUserId))
        {
            return Unauthorized();
        }

        var projectId = await _projectCreationRepository.CreateAsync(
            command,
            creatorUserId,
            cancellationToken);

        var result = new CreatedProject
        {
            ProjectId = projectId
        };

        return CreatedAtAction(
            nameof(Get),
            new { projectId },
            result);
    }
}
