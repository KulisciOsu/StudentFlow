namespace StudentFlow.Core.Kanban;

/// <summary>
/// Přístup k datům nástěnky.
/// </summary>
public interface IKanbanBoardRepository
{
    /// <summary>
    /// Vrátí projekt, nebo null, pokud neexistuje.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Projekt, nebo null.</returns>
    Task<KanbanProjectDto?> GetProjectAsync(int projectId);

    /// <summary>
    /// Vrátí sloupce projektu v zobrazovaném pořadí.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Sloupce projektu.</returns>
    Task<List<KanbanBoardColumnDto>> GetColumnsAsync(int projectId);

    /// <summary>
    /// Vrátí sloupec, nebo null, pokud neexistuje.
    /// </summary>
    /// <param name="columnId">Identifikátor sloupce.</param>
    /// <returns>Sloupec, nebo null.</returns>
    Task<KanbanBoardColumnDto?> GetColumnAsync(int columnId);

    /// <summary>
    /// Vrátí úkoly projektu.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Úkoly projektu.</returns>
    Task<List<KanbanBoardTaskDto>> GetTasksAsync(int projectId);

    /// <summary>
    /// Vrátí úkol, nebo null, pokud neexistuje.
    /// </summary>
    /// <param name="taskId">Identifikátor úkolu.</param>
    /// <returns>Úkol, nebo null.</returns>
    Task<KanbanBoardTaskDto?> GetTaskAsync(int taskId);

    /// <summary>
    /// Uloží sloupce a pozice úkolů projektu.
    /// </summary>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <param name="tasks">Úkoly projektu po změně.</param>
    Task SaveTasksAsync(int projectId, List<KanbanBoardTaskDto> tasks);
}
