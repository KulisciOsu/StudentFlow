using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Models;
using StudentFlow.Core.Kanban;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API úkolů nástěnky.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
public class TasksController : ControllerBase
{
    private readonly IKanbanBoardRepository _repository;

    /// <summary>
    /// Vytvoří controller úkolů.
    /// </summary>
    /// <param name="repository">Data nástěnky.</param>
    public TasksController(IKanbanBoardRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Vrací úkoly nástěnky projektu.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Seznam úkolů, nebo 404, pokud projekt neexistuje.</returns>
    [HttpGet]
    public async Task<ActionResult<List<KanbanBoardTaskDto>>> Get(int projectId)
    {
        var project = await _repository.GetProjectAsync(projectId);

        if (project is null)
        {
            return NotFound();
        }

        return await _repository.GetTasksAsync(projectId);
    }

    /// <summary>
    /// Přesune úkol do sloupce na danou pozici a uloží změnu.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <param name="taskId">Identifikátor úkolu.</param>
    /// <param name="request">Cílový sloupec a pozice.</param>
    /// <returns>204 při úspěchu, 400 při neplatném přesunu, 404 pokud úkol nebo sloupec neexistuje.</returns>
    [HttpPut("{taskId:int}/position")]
    public async Task<IActionResult> Move(int projectId, int taskId, MoveTaskRequestDto request)
    {
        var task = await _repository.GetTaskAsync(taskId);
        var column = await _repository.GetColumnAsync(request.ColumnId);
        var error = TaskMoveRules.Validate(projectId, task, column, request.Position);

        if (error == MoveTaskErrorEnum.TaskNotFound)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Úkol v projektu neexistuje."));
        }

        if (error == MoveTaskErrorEnum.ColumnNotFound)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, "Cílový sloupec neexistuje."));
        }

        if (error == MoveTaskErrorEnum.DifferentProject)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Cílový sloupec nepatří do projektu úkolu."));
        }

        if (error == MoveTaskErrorEnum.InvalidPosition)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, "Pozice nesmí být záporná."));
        }

        var tasks = await _repository.GetTasksAsync(projectId);
        var movedTasks = TaskMoveRules.Move(tasks, taskId, request.ColumnId, request.Position);

        await _repository.SaveTasksAsync(projectId, movedTasks);

        return NoContent();
    }

    /// <summary>
    /// Vytvoří popis chyby pro odpověď API.
    /// </summary>
    /// <param name="status">HTTP stavový kód.</param>
    /// <param name="title">Popis chyby.</param>
    /// <returns>Popis chyby.</returns>
    private static ProblemDetails CreateProblem(int status, string title)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
        };
    }
}
