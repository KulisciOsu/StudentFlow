
namespace StudentFlow.Api.Models;

/// <summary>
/// Souhrnná odpověď API pro úvodní dashboard uživatele.
/// </summary>
public sealed class DashboardDto
{
    /// <summary>Projekty, ve kterých je uživatel členem.</summary>
    public List<DashboardProjectDto> Projects { get; set; } = [];

    /// <summary>Úkoly přiřazené uživateli napříč projekty.</summary>
    public List<DashboardTaskDto> Tasks { get; set; } = [];
}
