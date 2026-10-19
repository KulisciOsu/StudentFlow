using StudentFlow.Api.Controllers;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru úkolů.
/// </summary>
public class TasksControllerTests
{
    /// <summary>
    /// Ověří výchozí úkoly a jejich sloupec.
    /// </summary>
    [Fact]
    public void Get_ReturnsDefaultTasksInFirstColumn()
    {
        var controller = new TasksController();

        var tasks = controller.Get();

        Assert.Collection(
            tasks,
            task =>
            {
                Assert.Equal(1, task.Id);
                Assert.Equal("Připravit návrh nástěnky", task.Title);
                Assert.Equal(1, task.ColumnId);
                Assert.Equal(0, task.Position);
            },
            task =>
            {
                Assert.Equal(2, task.Id);
                Assert.Equal("Ověřit přesouvání úkolů", task.Title);
                Assert.Equal(1, task.ColumnId);
                Assert.Equal(1, task.Position);
            });
    }
}
