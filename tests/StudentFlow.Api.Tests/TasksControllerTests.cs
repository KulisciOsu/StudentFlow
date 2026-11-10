using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;
using StudentFlow.Api.Models;

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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);

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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Ověří, že platný přesun se uloží.
    /// </summary>
    [Fact]
    public async Task Move_ValidRequest_SavesNewPosition()
    {
        var repository = new FakeKanbanBoardRepository();
        var controller = TestUsers.SignIn(new TasksController(repository), TestUsers.Leader);
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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);
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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);
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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);
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
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Leader);
        var request = new MoveTaskRequestDto { ColumnId = 99, Position = 0 };

        var result = await controller.Move(1, 2, request);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    /// <summary>
    /// Ověří, že člen vidí všechny úkoly, ale přesouvat smí jen své.
    /// </summary>
    [Fact]
    public async Task Get_Member_MarksOwnTasksAsMovable()
    {
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Member);

        var result = await controller.Get(1);

        Assert.Equal(10, result.Value!.Count);
        Assert.Equal(new[] { 4, 9 }, result.Value.Where(task => task.IsMine).Select(task => task.Id).Order());
        Assert.Equal(new[] { 4, 9 }, result.Value.Where(task => task.CanMove).Select(task => task.Id).Order());
    }

    /// <summary>
    /// Ověří, že vyučující nástěnku vidí, ale nesmí přesouvat žádný úkol.
    /// </summary>
    [Fact]
    public async Task Get_Teacher_CannotMoveAnyTask()
    {
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Teacher);

        var result = await controller.Get(1);

        Assert.Equal(10, result.Value!.Count);
        Assert.All(result.Value, task => Assert.False(task.CanMove));
    }

    /// <summary>
    /// Ověří, že uživatel mimo projekt dostane 403.
    /// </summary>
    [Fact]
    public async Task Get_Outsider_ReturnsForbidden()
    {
        var controller = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Outsider);

        var result = await controller.Get(1);

        Assert.IsType<ForbidResult>(result.Result);
    }

    /// <summary>
    /// Ověří, že člen přesune svůj úkol, ale cizí úkol mu API odmítne.
    /// </summary>
    [Fact]
    public async Task Move_Member_MovesOnlyOwnTask()
    {
        var repository = new FakeKanbanBoardRepository();
        var controller = TestUsers.SignIn(new TasksController(repository), TestUsers.Member);
        var request = new MoveTaskRequestDto { ColumnId = 3, Position = 0 };

        var own = await controller.Move(1, 4, request);
        var foreign = await controller.Move(1, 2, request);

        Assert.IsType<NoContentResult>(own);
        Assert.IsType<ForbidResult>(foreign);
        Assert.Equal(1, (await repository.GetTaskAsync(2))!.ColumnId);
    }

    /// <summary>
    /// Ověří, že vyučující ani uživatel mimo projekt nesmí přesouvat.
    /// </summary>
    [Fact]
    public async Task Move_TeacherOrOutsider_ReturnsForbidden()
    {
        var request = new MoveTaskRequestDto { ColumnId = 3, Position = 0 };
        var teacher = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Teacher);
        var outsider = TestUsers.SignIn(new TasksController(new FakeKanbanBoardRepository()), TestUsers.Outsider);

        var teacherResult = await teacher.Move(1, 2, request);
        var outsiderResult = await outsider.Move(1, 2, request);

        Assert.IsType<ForbidResult>(teacherResult);
        Assert.IsType<ForbidResult>(outsiderResult);
    }
}
