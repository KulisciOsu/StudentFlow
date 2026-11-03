using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;
using StudentFlow.Api.Models;
using StudentFlow.Data.Kanban;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru úkolů.
/// </summary>
public class TasksControllerTests
{
    /// <summary>
    /// Ověří, že se vrátí jen úkoly daného projektu.
    /// </summary>
    [Fact]
    public async Task Get_ExistingProject_ReturnsProjectTasksOnly()
    {
        var controller = new TasksController(new KanbanBoardRepository());

        var result = await controller.Get(1);

        Assert.NotNull(result.Value);
        Assert.Equal(10, result.Value.Count);
        Assert.All(result.Value, task => Assert.Equal(1, task.ProjectId));
    }

    /// <summary>
    /// Ověří, že neexistující projekt vrátí 404.
    /// </summary>
    [Fact]
    public async Task Get_UnknownProject_ReturnsNotFound()
    {
        var controller = new TasksController(new KanbanBoardRepository());

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Ověří, že platný přesun se uloží.
    /// </summary>
    [Fact]
    public async Task Move_ValidRequest_SavesNewPosition()
    {
        var repository = new KanbanBoardRepository();
        var controller = new TasksController(repository);
        var request = new MoveTaskRequestDto { ColumnId = 3, Position = 0 };

        var result = await controller.Move(1, 2, request);
        var task = await repository.GetTaskAsync(2);

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(task);
        Assert.Equal(3, task.ColumnId);
        Assert.Equal(0, task.Position);
    }

    /// <summary>
    /// Ověří, že přesun do sloupce jiného projektu vrátí 400.
    /// </summary>
    [Fact]
    public async Task Move_ColumnFromOtherProject_ReturnsBadRequest()
    {
        var controller = new TasksController(new KanbanBoardRepository());
        var request = new MoveTaskRequestDto { ColumnId = 5, Position = 0 };

        var result = await controller.Move(1, 2, request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Ověří, že záporná pozice vrátí 400.
    /// </summary>
    [Fact]
    public async Task Move_NegativePosition_ReturnsBadRequest()
    {
        var controller = new TasksController(new KanbanBoardRepository());
        var request = new MoveTaskRequestDto { ColumnId = 2, Position = -1 };

        var result = await controller.Move(1, 2, request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    /// <summary>
    /// Ověří, že neexistující úkol nebo úkol jiného projektu vrátí 404.
    /// </summary>
    [Fact]
    public async Task Move_UnknownTask_ReturnsNotFound()
    {
        var controller = new TasksController(new KanbanBoardRepository());
        var request = new MoveTaskRequestDto { ColumnId = 2, Position = 0 };

        var unknownTask = await controller.Move(1, 99, request);
        var otherProjectTask = await controller.Move(1, 11, request);

        Assert.IsType<NotFoundObjectResult>(unknownTask);
        Assert.IsType<NotFoundObjectResult>(otherProjectTask);
    }

    /// <summary>
    /// Ověří, že neexistující sloupec vrátí 404.
    /// </summary>
    [Fact]
    public async Task Move_UnknownColumn_ReturnsNotFound()
    {
        var controller = new TasksController(new KanbanBoardRepository());
        var request = new MoveTaskRequestDto { ColumnId = 99, Position = 0 };

        var result = await controller.Move(1, 2, request);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
