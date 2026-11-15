namespace StudentFlow.Api.Models;

/// <summary>
/// Odpověď 201 po přidání člena.
/// </summary>
/// <param name="Member">Nově přidaný člen.</param>
public sealed record ProjectMemberAddedDto(ProjectMemberDto Member);
