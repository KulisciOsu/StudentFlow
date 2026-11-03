namespace StudentFlow.Core.Kanban;

/// <summary>
/// Pravidla přesunu úkolu na nástěnce.
/// </summary>
public static class TaskMoveRules
{
    /// <summary>
    /// Ověří, že úkol i cílový sloupec existují a patří do stejného projektu.
    /// </summary>
    /// <param name="projectId">Projekt, ve kterém se přesouvá.</param>
    /// <param name="task">Přesouvaný úkol, nebo null.</param>
    /// <param name="column">Cílový sloupec, nebo null.</param>
    /// <param name="position">Cílová pozice.</param>
    /// <returns>Důvod neplatnosti, nebo <see cref="MoveTaskErrorEnum.None"/>.</returns>
    public static MoveTaskErrorEnum Validate(int projectId, KanbanBoardTaskDto? task, KanbanBoardColumnDto? column, int position)
    {
        if (task is null || task.ProjectId != projectId)
        {
            return MoveTaskErrorEnum.TaskNotFound;
        }

        if (column is null)
        {
            return MoveTaskErrorEnum.ColumnNotFound;
        }

        if (column.ProjectId != task.ProjectId)
        {
            return MoveTaskErrorEnum.DifferentProject;
        }

        if (position < 0)
        {
            return MoveTaskErrorEnum.InvalidPosition;
        }

        return MoveTaskErrorEnum.None;
    }

    /// <summary>
    /// Přesune úkol do sloupce na danou pozici a přečísluje pozice ve zdrojovém i cílovém sloupci.
    /// Pozice mimo rozsah se umístí na konec sloupce.
    /// </summary>
    /// <param name="tasks">Úkoly projektu.</param>
    /// <param name="taskId">Přesouvaný úkol.</param>
    /// <param name="columnId">Cílový sloupec.</param>
    /// <param name="position">Cílová pozice.</param>
    /// <returns>Úkoly po přesunu.</returns>
    public static List<KanbanBoardTaskDto> Move(List<KanbanBoardTaskDto> tasks, int taskId, int columnId, int position)
    {
        var task = tasks.FirstOrDefault(item => item.Id == taskId);

        if (task is null)
        {
            return tasks;
        }

        var remaining = tasks
            .Where(item => item.Id != taskId)
            .ToList();

        var destination = remaining
            .Where(item => item.ColumnId == columnId)
            .OrderBy(item => item.Position)
            .ToList();

        var targetPosition = Math.Clamp(position, 0, destination.Count);
        destination.Insert(targetPosition, task);

        var source = new List<KanbanBoardTaskDto>();

        if (task.ColumnId != columnId)
        {
            source = remaining
                .Where(item => item.ColumnId == task.ColumnId)
                .OrderBy(item => item.Position)
                .ToList();
        }

        var result = remaining
            .Where(item => item.ColumnId != task.ColumnId && item.ColumnId != columnId)
            .ToList();

        for (var index = 0; index < source.Count; index++)
        {
            result.Add(Place(source[index], source[index].ColumnId, index));
        }

        for (var index = 0; index < destination.Count; index++)
        {
            result.Add(Place(destination[index], columnId, index));
        }

        return result;
    }

    /// <summary>
    /// Vytvoří kopii úkolu s novým sloupcem a pozicí.
    /// </summary>
    /// <param name="task">Původní úkol.</param>
    /// <param name="columnId">Nový sloupec.</param>
    /// <param name="position">Nová pozice.</param>
    /// <returns>Kopie úkolu.</returns>
    private static KanbanBoardTaskDto Place(KanbanBoardTaskDto task, int columnId, int position)
    {
        return new KanbanBoardTaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            ColumnId = columnId,
            Position = position,
            Title = task.Title,
            Priority = task.Priority,
            Assignee = task.Assignee,
            DueDate = task.DueDate,
        };
    }
}
