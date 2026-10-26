using StudentFlow.Core.Entities;
using StudentFlow.Core.Interfaces;

namespace StudentFlow.Data.Repositories;

/// <summary>
/// Úložiště projektů. Zatím dočasně nic neukládá, jen přiděluje identifikátory.
/// TODO: nahradit Dapperem a PostgreSQL (vložení projektu, týmu a členů v jedné transakci).
/// </summary>
public class ProjectRepository : IProjectRepository
{
    /// <summary>
    /// Poslední přidělený identifikátor.
    /// </summary>
    private static int _lastId;

    /// <summary>
    /// Přidělí projektu a jeho týmu identifikátor.
    /// </summary>
    /// <param name="project">Nový projekt s týmem a členy.</param>
    /// <param name="cancellationToken">Token pro zrušení operace.</param>
    /// <returns>Úloha, která skončí po uložení.</returns>
    public Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        project.Id = Interlocked.Increment(ref _lastId);
        project.Team.Id = project.Id;
        return Task.CompletedTask;
    }
}
