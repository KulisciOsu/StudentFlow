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
            new KanbanBoardColumnDto { Id = 1, Title = "To Do" },
            new KanbanBoardColumnDto { Id = 2, Title = "In Progress" },
            new KanbanBoardColumnDto { Id = 3, Title = "Done" }
        ];
    }
}
