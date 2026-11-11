
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StudentFlow.Api.Controllers;
using StudentFlow.Core.Projects;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru projektů.
/// </summary>
public class ProjectsControllerTests
{
    [Fact]
    public async Task Get_ExistingProject_ReturnsProject()
    {
        var controller = CreateController();

        var result = await controller.Get(1);

        Assert.NotNull(result.Value);
        Assert.Equal("Rezervační systém knihovny", result.Value.Name);
    }

    [Fact]
    public async Task Get_UnknownProject_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Get(99);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ValidCommand_ReturnsCreatedProject()
    {
        var repository = new FakeProjectCreationRepository();
        var controller = CreateController(repository);

        SetAuthenticatedUser(controller, "42");

        var command = new CreateProjectCommand
        {
            Name = "Nový projekt",
            TeamName = "Vývojový tým"
        };

        var result = await controller.Create(command, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var project = Assert.IsType<CreatedProject>(createdResult.Value);

        Assert.Equal(1, project.ProjectId);
        Assert.Equal(42, repository.CreatorUserId);
        Assert.Equal("Nový projekt", repository.ReceivedCommand?.Name);
        Assert.Equal("Vývojový tým", repository.ReceivedCommand?.TeamName);
    }

    [Fact]
    public async Task Create_WithoutUserId_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var command = new CreateProjectCommand
        {
            Name = "Nový projekt",
            TeamName = "Vývojový tým"
        };

        var result = await controller.Create(command, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task Create_WithoutProjectName_ReturnsBadRequest()
    {
        var repository = new FakeProjectCreationRepository();
        var controller = CreateController(repository);

        SetAuthenticatedUser(controller, "42");

        var command = new CreateProjectCommand
        {
            Name = "",
            TeamName = "Vývojový tým"
        };

        var result = await controller.Create(command, CancellationToken.None);

        var badRequest = Assert.IsType<ObjectResult>(result.Result);
        var problem = Assert.IsAssignableFrom<ProblemDetails>(badRequest.Value);

        var validationProblem = Assert.IsType<ValidationProblemDetails>(problem);

        Assert.Contains(nameof(command.Name), validationProblem.Errors.Keys);
        Assert.Null(repository.ReceivedCommand);
    }

    private static ProjectsController CreateController(
        FakeProjectCreationRepository? repository = null)
    {
        var controller = new ProjectsController(
            new FakeKanbanBoardRepository(),
            repository ?? new FakeProjectCreationRepository());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        return controller;
    }

    private static void SetAuthenticatedUser(
        ProjectsController controller,
        string userId)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            },
            "TestAuthentication");

        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(identity);
    }

    private sealed class FakeProjectCreationRepository : IProjectCreationRepository
    {
        public long CreatorUserId { get; private set; }

        public CreateProjectCommand? ReceivedCommand { get; private set; }

        public Task<int> CreateAsync(
            CreateProjectCommand command,
            long creatorUserId,
            CancellationToken cancellationToken)
        {
            ReceivedCommand = command;
            CreatorUserId = creatorUserId;

            return Task.FromResult(1);
        }
    }
}
