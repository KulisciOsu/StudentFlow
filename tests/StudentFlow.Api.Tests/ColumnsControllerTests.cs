using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;
using StudentFlow.Data.Kanban;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru sloupců.
/// </summary>
public class ColumnsControllerTests
{
    /// <summary>
    /// Ověří, že se vrátí jen sloupce daného projektu v pořadí.
    /// </summary>
    [Fact]
    public async Task Get_ExistingProject_ReturnsProjectColumnsInOrder()
    {
        var controller = new ColumnsController(new KanbanBoardRepository());

        var result = await controller.Get(1);

        Assert.NotNull(result.Value);
        Assert.Equal(new[] { "K řešení", "Rozpracováno", "Ke kontrole", "Hotovo" }, result.Value.Select(column => column.Title));
        Assert.All(result.Value, column => Assert.Equal(1, column.ProjectId));
    }

    /// <summary>
    /// Ověří, že neexistující projekt vrátí 404.
    /// </summary>
    [Fact]
    public async Task Get_UnknownProject_ReturnsNotFound()
    {
        var controller = new ColumnsController(new KanbanBoardRepository());

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
