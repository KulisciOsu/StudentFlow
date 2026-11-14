namespace StudentFlow.Api.Models;

/// <summary>
/// Odpověď se seznamem členů projektu: název týmu pod nadpisem a členové, vedoucí jako první.
/// </summary>
/// <param name="TeamName">Název týmu.</param>
/// <param name="Members">Aktuální členové projektu.</param>
public sealed record ProjectMembersDto(string TeamName, IReadOnlyList<ProjectMemberDto> Members);
