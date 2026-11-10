using System.Globalization;
using System.Security.Claims;
using StudentFlow.Core.Authentication;
using StudentFlow.Core.Kanban;

namespace StudentFlow.Api.Controllers;

/// <summary>
/// Zjišťuje přihlášeného uživatele a jeho roli vůči projektu nástěnky.
/// </summary>
public static class BoardAccess
{
    /// <summary>
    /// Vrátí identifikátor přihlášeného uživatele.
    /// </summary>
    /// <param name="user">Přihlášený uživatel.</param>
    /// <returns>Identifikátor uživatele.</returns>
    public static long GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return long.Parse(value!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Vrátí roli přihlášeného uživatele vůči projektu.
    /// </summary>
    /// <param name="repository">Data nástěnky.</param>
    /// <param name="user">Přihlášený uživatel.</param>
    /// <param name="projectId">Identifikátor projektu.</param>
    /// <returns>Role uživatele.</returns>
    public static async Task<ProjectRoleEnum> GetRoleAsync(IKanbanBoardRepository repository, ClaimsPrincipal user, int projectId)
    {
        var isTeacher = user.IsInRole(AccountTypeCodes.ToCode(AccountType.Teacher));
        var isLeader = await repository.GetMembershipAsync(projectId, GetUserId(user));

        return BoardPermissions.GetRole(isTeacher, isLeader);
    }
}
