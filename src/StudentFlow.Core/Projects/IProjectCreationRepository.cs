namespace StudentFlow.Core.Projects;

/// <summary>
/// Definuje operace potřebné pro založení projektu.
/// </summary>
public interface IProjectCreationRepository
{
    /// <summary>
    /// Založí projekt, přidá jeho vedoucího a vytvoří výchozí Kanban sloupce.
    /// </summary>
    /// <param name="command">Údaje nového projektu.</param>
    /// <param name="creatorUserId">ID přihlášeného uživatele, který projekt zakládá.</param>
    /// <param name="cancellationToken">Token pro zrušení operace.</param>
    /// <returns>ID nově vytvořeného projektu.</returns>
    Task<int> CreateAsync(
        CreateProjectCommand command,
        long creatorUserId,
        CancellationToken cancellationToken);
}