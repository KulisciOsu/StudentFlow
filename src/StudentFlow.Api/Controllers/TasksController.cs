using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Models;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API úkolů nástěnky.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    /// <summary>
    /// Vrací úkoly nástěnky.
    /// </summary>
    /// <returns>Seznam úkolů.</returns>
    [HttpGet]
    public List<KanbanBoardTaskDto> Get()
    {
        return
        [
            new KanbanBoardTaskDto { Id = 1, Title = "Připravit návrh nástěnky", ColumnId = 1, Position = 0 },
            new KanbanBoardTaskDto { Id = 2, Title = "Ověřit přesouvání úkolů", ColumnId = 1, Position = 1 }
        ];
    }
}
