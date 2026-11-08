namespace StudentFlow.Core.Kanban;

/// <summary>
/// Důvod, proč nelze úkol přesunout.
/// </summary>
public enum MoveTaskErrorEnum
{
    /// <summary>
    /// Přesun je platný.
    /// </summary>
    None,

    /// <summary>
    /// Úkol v projektu neexistuje.
    /// </summary>
    TaskNotFound,

    /// <summary>
    /// Cílový sloupec neexistuje.
    /// </summary>
    ColumnNotFound,

    /// <summary>
    /// Úkol a cílový sloupec patří do různých projektů.
    /// </summary>
    DifferentProject,

    /// <summary>
    /// Cílová pozice je záporná.
    /// </summary>
    InvalidPosition,
}
