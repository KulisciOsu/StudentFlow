namespace StudentFlow.Core.ProjectMembers;

/// <summary>Aktuální členství jednoho uživatele v jednom projektu (CLEN_PROJEKTU z ER modelu).</summary>
/// <param name="UserId">Identifikátor účtu člena.</param>
/// <param name="DisplayName">Zobrazované jméno člena.</param>
/// <param name="Email">Uložený e-mail člena.</param>
/// <param name="IsLeader">Zda je člen vedoucím právě tohoto projektu; projekt má právě jednoho vedoucího.</param>
/// <param name="AddedAt">Okamžik přidání do projektu.</param>
public sealed record ProjectMember(long UserId, string DisplayName, string Email, bool IsLeader, DateTimeOffset AddedAt);
