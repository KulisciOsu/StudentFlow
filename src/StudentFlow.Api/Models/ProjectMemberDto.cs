using System.Globalization;
using StudentFlow.Core.ProjectMembers;

namespace StudentFlow.Api.Models;

/// <summary>
/// Člen projektu v odpovědi API; ID uživatele je text, aby ho JavaScript nezaokrouhlil.
/// </summary>
/// <param name="UserId">Identifikátor uživatele jako text.</param>
/// <param name="DisplayName">Zobrazované jméno.</param>
/// <param name="Email">E-mail účtu.</param>
/// <param name="IsLeader">Zda je člen vedoucím týmu.</param>
/// <param name="JoinedAt">Okamžik vstupu do projektu.</param>
public sealed record ProjectMemberDto(string UserId, string DisplayName, string Email, bool IsLeader, DateTimeOffset JoinedAt)
{
    /// <summary>
    /// Převede doménové členství na odpověď API.
    /// </summary>
    public static ProjectMemberDto From(ProjectMember member) => new(
        member.UserId.ToString(CultureInfo.InvariantCulture), member.DisplayName, member.Email, member.IsLeader, member.JoinedAt);
}
