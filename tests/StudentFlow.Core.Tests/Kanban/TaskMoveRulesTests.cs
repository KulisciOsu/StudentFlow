using StudentFlow.Core.Kanban;

namespace StudentFlow.Core.Tests.Kanban;

/// <summary>
/// Testy pravidel přesunu úkolu.
/// </summary>
public class TaskMoveRulesTests
{
    private static readonly KanbanBoardColumnDto Column = CreateColumn(2, 1);

    private static readonly List<KanbanBoardTaskDto> Tasks =
    [
        CreateTask(1, 1, 0),
        CreateTask(2, 1, 1),
        CreateTask(3, 1, 2),
        CreateTask(4, 2, 0),
    ];

    /// <summary>
    /// Vytvoří testovací úkol.
    /// </summary>
    private static KanbanBoardTaskDto CreateTask(int id, int columnId, int position, int projectId = 1)
    {
        return new KanbanBoardTaskDto
        {
            Id = id,
            ProjectId = projectId,
            ColumnId = columnId,
            Position = position,
            Title = $"Úkol {id}",
            Assignee = "Jan Novák",
        };
    }

    /// <summary>
    /// Vytvoří testovací sloupec.
    /// </summary>
    private static KanbanBoardColumnDto CreateColumn(int id, int projectId)
    {
        return new KanbanBoardColumnDto
        {
            Id = id,
            ProjectId = projectId,
            Title = $"Sloupec {id}",
            IsDone = false,
        };
    }

    /// <summary>
    /// Vrátí úkoly sloupce ve tvaru „id@pozice“ seřazené podle pozice.
    /// </summary>
    private static string[] ColumnOrder(List<KanbanBoardTaskDto> tasks, int columnId)
    {
        return tasks
            .Where(task => task.ColumnId == columnId)
            .OrderBy(task => task.Position)
            .Select(task => $"{task.Id}@{task.Position}")
            .ToArray();
    }

    /// <summary>
    /// Ověří, že platný přesun projde validací.
    /// </summary>
    [Fact]
    public void Validate_ValidMove_ReturnsNone()
    {
        var task = CreateTask(1, 1, 0);

        var result = TaskMoveRules.Validate(1, task, Column, 0);

        Assert.Equal(MoveTaskErrorEnum.None, result);
    }

    /// <summary>
    /// Ověří, že sloupec z jiného projektu je odmítnut.
    /// </summary>
    [Fact]
    public void Validate_ColumnFromOtherProject_ReturnsDifferentProject()
    {
        var task = CreateTask(1, 1, 0);
        var otherColumn = CreateColumn(9, 2);

        var result = TaskMoveRules.Validate(1, task, otherColumn, 0);

        Assert.Equal(MoveTaskErrorEnum.DifferentProject, result);
    }

    /// <summary>
    /// Ověří, že úkol z jiného projektu se nenajde.
    /// </summary>
    [Fact]
    public void Validate_TaskFromOtherProject_ReturnsTaskNotFound()
    {
        var task = CreateTask(1, 1, 0, projectId: 2);

        var result = TaskMoveRules.Validate(1, task, Column, 0);

        Assert.Equal(MoveTaskErrorEnum.TaskNotFound, result);
    }

    /// <summary>
    /// Ověří chybějící úkol a chybějící sloupec.
    /// </summary>
    [Fact]
    public void Validate_MissingTaskOrColumn_ReturnsNotFound()
    {
        var task = CreateTask(1, 1, 0);

        var missingTask = TaskMoveRules.Validate(1, null, Column, 0);
        var missingColumn = TaskMoveRules.Validate(1, task, null, 0);

        Assert.Equal(MoveTaskErrorEnum.TaskNotFound, missingTask);
        Assert.Equal(MoveTaskErrorEnum.ColumnNotFound, missingColumn);
    }

    /// <summary>
    /// Ověří, že záporná pozice je odmítnuta.
    /// </summary>
    [Fact]
    public void Validate_NegativePosition_ReturnsInvalidPosition()
    {
        var task = CreateTask(1, 1, 0);

        var result = TaskMoveRules.Validate(1, task, Column, -1);

        Assert.Equal(MoveTaskErrorEnum.InvalidPosition, result);
    }

    /// <summary>
    /// Ověří přesun do jiného sloupce a přečíslování obou sloupců.
    /// </summary>
    [Fact]
    public void Move_ToOtherColumn_ReindexesBothColumns()
    {
        var result = TaskMoveRules.Move(Tasks, 2, 2, 0);

        Assert.Equal(new[] { "1@0", "3@1" }, ColumnOrder(result, 1));
        Assert.Equal(new[] { "2@0", "4@1" }, ColumnOrder(result, 2));
    }

    /// <summary>
    /// Ověří změnu pořadí ve stejném sloupci.
    /// </summary>
    [Fact]
    public void Move_WithinColumn_ReordersTasks()
    {
        var result = TaskMoveRules.Move(Tasks, 1, 1, 2);

        Assert.Equal(new[] { "2@0", "3@1", "1@2" }, ColumnOrder(result, 1));
    }

    /// <summary>
    /// Ověří, že pozice mimo rozsah umístí úkol na konec sloupce.
    /// </summary>
    [Fact]
    public void Move_PositionOutOfRange_PlacesTaskAtEnd()
    {
        var result = TaskMoveRules.Move(Tasks, 1, 2, 99);

        Assert.Equal(new[] { "4@0", "1@1" }, ColumnOrder(result, 2));
    }

    /// <summary>
    /// Ověří, že neznámý úkol nechá úkoly beze změny.
    /// </summary>
    [Fact]
    public void Move_UnknownTask_ReturnsOriginalTasks()
    {
        var result = TaskMoveRules.Move(Tasks, 42, 1, 0);

        Assert.Same(Tasks, result);
    }
}
