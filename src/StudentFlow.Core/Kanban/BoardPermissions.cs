namespace StudentFlow.Core.Kanban;

/// <summary>
/// Oprávnění na nástěnce projektu.
/// </summary>
public static class BoardPermissions
{
    /// <summary>
    /// Určí roli uživatele vůči projektu.
    /// </summary>
    /// <param name="isTeacher">Zda je účet vyučující.</param>
    /// <param name="isLeader">Členství v projektu: true vedoucí, false člen, null není členem.</param>
    /// <returns>Role uživatele.</returns>
    public static ProjectRoleEnum GetRole(bool isTeacher, bool? isLeader)
    {
        if (isTeacher)
        {
            return ProjectRoleEnum.Teacher;
        }

        if (isLeader is null)
        {
            return ProjectRoleEnum.None;
        }

        if (isLeader.Value)
        {
            return ProjectRoleEnum.Leader;
        }

        return ProjectRoleEnum.Member;
    }

    /// <summary>
    /// Zda uživatel smí nástěnku zobrazit.
    /// </summary>
    /// <param name="role">Role uživatele.</param>
    /// <returns>True, pokud smí nástěnku zobrazit.</returns>
    public static bool CanView(ProjectRoleEnum role)
    {
        return role != ProjectRoleEnum.None;
    }

    /// <summary>
    /// Zda má uživatel nástěnku jen pro čtení.
    /// </summary>
    /// <param name="role">Role uživatele.</param>
    /// <returns>True pro vyučujícího a uživatele bez přístupu.</returns>
    public static bool IsReadOnly(ProjectRoleEnum role)
    {
        return role != ProjectRoleEnum.Leader && role != ProjectRoleEnum.Member;
    }

    /// <summary>
    /// Zda uživatel smí přesunout úkol. Vedoucí přesouvá všechny úkoly, člen jen své.
    /// </summary>
    /// <param name="role">Role uživatele.</param>
    /// <param name="userId">Identifikátor uživatele.</param>
    /// <param name="task">Přesouvaný úkol.</param>
    /// <returns>True, pokud smí úkol přesunout.</returns>
    public static bool CanMove(ProjectRoleEnum role, long userId, KanbanBoardTaskDto task)
    {
        if (role == ProjectRoleEnum.Leader)
        {
            return true;
        }

        if (role == ProjectRoleEnum.Member)
        {
            return task.AssigneeId == userId;
        }

        return false;
    }
}
