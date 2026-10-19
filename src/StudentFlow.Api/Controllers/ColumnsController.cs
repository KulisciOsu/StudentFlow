using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Models;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// API sloupců nástěnky.
/// </summary>
[ApiController]
[Route("api/columns")]
public class ColumnsController : ControllerBase
{
    /// <summary>
    /// Vrací sloupce nástěnky.
    /// </summary>
    /// <returns>Seznam sloupců.</returns>
    [HttpGet]
    public List<KanbanBoardColumnDto> Get()
    {
        return
        [
            new KanbanBoardColumnDto { Id = "todo", Title = "To Do" },
            new KanbanBoardColumnDto { Id = "in-progress", Title = "In Progress" },
            new KanbanBoardColumnDto { Id = "done", Title = "Done" }
        ];
    }
}
