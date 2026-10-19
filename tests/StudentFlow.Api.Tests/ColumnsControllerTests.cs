using StudentFlow.Api.Controllers;

namespace StudentFlow.Api.Tests;

/// <summary>
/// Testy controlleru sloupců.
/// </summary>
public class ColumnsControllerTests
{
    /// <summary>
    /// Ověří výchozí sloupce a jejich pořadí.
    /// </summary>
    [Fact]
    public void Get_ReturnsDefaultColumnsInOrder()
    {
        var controller = new ColumnsController();

        var columns = controller.Get();

        Assert.Collection(
            columns,
            column =>
            {
                Assert.Equal(1, column.Id);
                Assert.Equal("To Do", column.Title);
            },
            column =>
            {
                Assert.Equal(2, column.Id);
                Assert.Equal("In Progress", column.Title);
            },
            column =>
            {
                Assert.Equal(3, column.Id);
                Assert.Equal("Done", column.Title);
            });
    }
}
