using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;

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
        var controller = TestUsers.SignIn(new ProjectsController(new FakeKanbanBoardRepository()), TestUsers.Leader);

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
        var controller = TestUsers.SignIn(new ProjectsController(new FakeKanbanBoardRepository()), TestUsers.Leader);

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    /// <summary>
    /// Ověří, že uživatel mimo projekt dostane 403.
    /// </summary>
    [Fact]
    public async Task Get_Outsider_ReturnsForbidden()
    {
        var controller = TestUsers.SignIn(new ProjectsController(new FakeKanbanBoardRepository()), TestUsers.Outsider);

        var result = await controller.Get(1);

        Assert.IsType<ForbidResult>(result.Result);
    }

    /// <summary>
    /// Ověří, že vyučující má nástěnku jen pro čtení a člen ne.
    /// </summary>
    [Fact]
    public async Task Get_TeacherAndMember_ReturnsReadOnlyFlag()
    {
        var teacher = TestUsers.SignIn(new ProjectsController(new FakeKanbanBoardRepository()), TestUsers.Teacher);
        var member = TestUsers.SignIn(new ProjectsController(new FakeKanbanBoardRepository()), TestUsers.Member);

        var teacherResult = await teacher.Get(1);
        var memberResult = await member.Get(1);

        Assert.True(teacherResult.Value!.IsReadOnly);
        Assert.False(memberResult.Value!.IsReadOnly);
    }
}
