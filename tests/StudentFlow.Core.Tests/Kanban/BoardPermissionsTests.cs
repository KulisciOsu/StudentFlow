using StudentFlow.Core.Kanban;

namespace StudentFlow.Core.Tests.Kanban;

/// <summary>
/// Testy oprávnění na nástěnce.
/// </summary>
public class BoardPermissionsTests
{
    private const long UserId = 7;

    /// <summary>
    /// Ověří určení role podle typu účtu a členství v projektu.
    /// </summary>
    [Theory]
    [InlineData(false, null, ProjectRoleEnum.None)]
    [InlineData(false, false, ProjectRoleEnum.Member)]
    [InlineData(false, true, ProjectRoleEnum.Leader)]
    [InlineData(true, null, ProjectRoleEnum.Teacher)]
    [InlineData(true, true, ProjectRoleEnum.Teacher)]
    public void GetRole_ReturnsRoleByAccountAndMembership(bool isTeacher, bool? isLeader, ProjectRoleEnum expected)
    {
        var role = BoardPermissions.GetRole(isTeacher, isLeader);

        Assert.Equal(expected, role);
    }

    /// <summary>
    /// Ověří, kdo smí nástěnku zobrazit a kdo ji má jen pro čtení.
    /// </summary>
    [Theory]
    [InlineData(ProjectRoleEnum.None, false, true)]
    [InlineData(ProjectRoleEnum.Member, true, false)]
    [InlineData(ProjectRoleEnum.Leader, true, false)]
    [InlineData(ProjectRoleEnum.Teacher, true, true)]
    public void CanViewAndIsReadOnly_ByRole(ProjectRoleEnum role, bool canView, bool isReadOnly)
    {
        Assert.Equal(canView, BoardPermissions.CanView(role));
        Assert.Equal(isReadOnly, BoardPermissions.IsReadOnly(role));
    }

    /// <summary>
    /// Ověří, že vedoucí přesouvá všechny úkoly, člen jen své a ostatní nic.
    /// </summary>
    [Theory]
    [InlineData(ProjectRoleEnum.Leader, UserId, true)]
    [InlineData(ProjectRoleEnum.Leader, 99, true)]
    [InlineData(ProjectRoleEnum.Member, UserId, true)]
    [InlineData(ProjectRoleEnum.Member, 99, false)]
    [InlineData(ProjectRoleEnum.Teacher, UserId, false)]
    [InlineData(ProjectRoleEnum.None, UserId, false)]
    public void CanMove_ByRoleAndAssignee(ProjectRoleEnum role, long assigneeId, bool expected)
    {
        var task = new KanbanBoardTaskDto
        {
            Id = 1,
            ProjectId = 1,
            ColumnId = 1,
            Position = 0,
            Title = "Úkol",
            Assignee = "Jan Novák",
            AssigneeId = assigneeId,
        };

        var canMove = BoardPermissions.CanMove(role, UserId, task);

        Assert.Equal(expected, canMove);
    }
}
