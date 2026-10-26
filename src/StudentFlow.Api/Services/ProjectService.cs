using StudentFlow.Api.DTOs;
using StudentFlow.Core.Entities;
using StudentFlow.Core.Enums;
using StudentFlow.Core.Interfaces;

namespace StudentFlow.Api.Services;

/// <summary>
/// Logika práce s projekty.
/// </summary>
/// <param name="projects">Úložiště projektů.</param>
public class ProjectService(IProjectRepository projects)
{
    /// <summary>
    /// Založí projekt a tým. Zakladatel se stane vedoucím týmu.
    /// </summary>
    /// <param name="request">Ověřené údaje projektu a týmu.</param>
    /// <param name="founderUserId">Identifikátor zakladatele.</param>
    /// <param name="cancellationToken">Token pro zrušení operace.</param>
    /// <returns>Založený projekt s vyplněnými identifikátory.</returns>
    public async Task<Project> CreateAsync(
        CreateProjectRequestDto request, int founderUserId, CancellationToken cancellationToken)
    {
        var deadline = request.Deadline
            ?? throw new ArgumentException("Termín odevzdání je povinný.", nameof(request));
        var description = request.Description?.Trim();

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrEmpty(description) ? null : description,
            Deadline = deadline,
            Team = new Team
            {
                Name = request.TeamName.Trim(),
                Members = [new TeamMember { UserId = founderUserId, Role = TeamRole.Leader }],
            },
        };

        await projects.AddAsync(project, cancellationToken);
        return project;
    }
}
