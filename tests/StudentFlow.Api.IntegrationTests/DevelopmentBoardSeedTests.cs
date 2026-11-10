using StudentFlow.Api.Development;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Ověřuje zapojení vývojových účtů do ukázkového projektu nástěnky.
/// </summary>
public class DevelopmentBoardSeedTests
{
    /// <summary>
    /// Ověří, že člen a vedoucí jsou v ukázkovém projektu s vlastními úkoly a opakovaný seed nic nezdvojí.
    /// </summary>
    [Fact]
    public async Task Seed_WithDemoBoard_JoinsMemberAndLeaderWithOwnTasks()
    {
        var memberEmail = $"member-{Guid.NewGuid():N}@example.invalid";
        var leaderEmail = $"leader-{Guid.NewGuid():N}@example.invalid";
        var settings = new Dictionary<string, string?>
        {
            ["DevelopmentSeed:MemberEmail"] = memberEmail,
            ["DevelopmentSeed:TeacherEmail"] = $"teacher-{Guid.NewGuid():N}@example.invalid",
            ["DevelopmentSeed:LeaderEmail"] = leaderEmail,
            ["DevelopmentSeed:Password"] = Guid.NewGuid().ToString("N"),
        };
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: true, seedConfiguration: settings, dashboard: true);
        await app.Database.ApplyMigrationAsync("V005__seed_kanban_demo.sql");
        await app.Database.ApplyMigrationAsync("V007__seed_kanban_demo_members.sql");

        await DevelopmentAccountSeeder.SeedAsync(app.Services);
        await DevelopmentAccountSeeder.SeedAsync(app.Services);

        Assert.Equal(2L, await CountTasksAsync(app, memberEmail));
        Assert.Equal(2L, await CountTasksAsync(app, leaderEmail));
        Assert.Equal(0L, await CountTasksAsync(app, "jan.novak@studentflow.local"));
        Assert.Equal(7L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM project_member"));
        Assert.Equal(leaderEmail, await app.Database.ScalarAsync<string>("""
            SELECT app_user.email
            FROM project_member
            JOIN app_user ON app_user.user_id = project_member.user_id
            WHERE project_member.is_leader
            """));
    }

    /// <summary>
    /// Vrátí počet úkolů přiřazených účtu.
    /// </summary>
    private static Task<long> CountTasksAsync(RegistrationApp app, string email)
    {
        return app.Database.ScalarAsync<long>($"""
            SELECT count(*)
            FROM task
            JOIN app_user ON app_user.user_id = task.assignee_id
            WHERE app_user.email = '{email}'
            """);
    }
}
