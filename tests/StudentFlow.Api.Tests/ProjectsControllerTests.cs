using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;
using StudentFlow.Data.Kanban;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru projektů.
/// </summary>
public class ProjectsControllerTests
{
    /// <summary>
    /// Ověří, že existující projekt se vrátí.
    /// </summary>
    [Fact]
    public async Task Get_ExistingProject_ReturnsProject()
    {
        var controller = new ProjectsController(new KanbanBoardRepository());

        var result = await controller.Get(1);

        Assert.NotNull(result.Value);
        Assert.Equal("Rezervační systém knihovny", result.Value.Name);
    }

    /// <summary>
    /// Ověří, že neexistující projekt vrátí 404.
    /// </summary>
    [Fact]
    public async Task Get_UnknownProject_ReturnsNotFound()
    {
        var controller = new ProjectsController(new KanbanBoardRepository());

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
