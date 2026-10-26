using StudentFlow.Core.Entities;

namespace StudentFlow.Core.Interfaces;

/// <summary>
/// Úložiště projektů.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Uloží projekt spolu s týmem a jeho členy v jedné transakci a doplní vygenerovaná Id.
    /// </summary>
    /// <param name="project">Nový projekt s týmem a členy.</param>
    /// <param name="cancellationToken">Token pro zrušení operace.</param>
    /// <returns>Úloha, která skončí po uložení.</returns>
    Task AddAsync(Project project, CancellationToken cancellationToken);
}
