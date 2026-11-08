
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Integrační testy API úvodního dashboardu.
/// </summary>
public class DashboardControllerTests
{
    /// <summary>
    /// Nepřihlášený uživatel nesmí získat data dashboardu.
    /// </summary>
    [Fact]
    public async Task GetDashboard_AnonymousUser_Returns401()
    {
        await using var app = await RegistrationApp.CreateAsync();

        var response = await app.Client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Nově registrovaný uživatel má prázdný dashboard.
    /// </summary>
    [Fact]
    public async Task GetDashboard_AuthenticatedUser_ReturnsEmptyDashboard()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        var registration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid());

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await app.Client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "jan.novak@example.test",
                password = "moje-dlouhe-heslo-2026"
            });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await app.Client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, json.GetProperty("projects").GetArrayLength());
        Assert.Equal(0, json.GetProperty("tasks").GetArrayLength());
    }

    /// <summary>
    /// Dashboard vrací pouze projekty a úkoly přihlášeného člena.
    /// </summary>
    [Fact]
    public async Task GetDashboard_ReturnsOnlyCurrentUsersData()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        // Registrace dvou uživatelů.
        var firstRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("jan.novak@example.test"));

        Assert.Equal(
            HttpStatusCode.Created,
            firstRegistration.StatusCode);

        var secondRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("eva.svobodova@example.test"));

        Assert.Equal(
            HttpStatusCode.Created,
            secondRegistration.StatusCode);

        // Vytvoření projektů a úkolů v izolované testovací DB.
        await app.Database.ExecuteAsync("""
            DO $$
            DECLARE
                first_user BIGINT;
                second_user BIGINT;
                first_project INTEGER;
                second_project INTEGER;
                first_column INTEGER;
                second_column INTEGER;
            BEGIN
                SELECT user_id INTO first_user
                FROM app_user
                WHERE email = 'jan.novak@example.test';

                SELECT user_id INTO second_user
                FROM app_user
                WHERE email = 'eva.svobodova@example.test';

                INSERT INTO project (name, team_name, due_date)
                VALUES ('Projekt Jana', 'Tým A', DATE '2027-01-15')
                RETURNING project_id INTO first_project;

                INSERT INTO project (name, team_name)
                VALUES ('Projekt Evy', 'Tým B')
                RETURNING project_id INTO second_project;

                INSERT INTO project_member
                    (project_id, user_id, is_leader)
                VALUES
                    (first_project, first_user, TRUE),
                    (second_project, second_user, TRUE);

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES
                    (first_project, 'K řešení', 0, FALSE)
                RETURNING column_id INTO first_column;

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES
                    (second_project, 'K řešení', 0, FALSE)
                RETURNING column_id INTO second_column;

                INSERT INTO task
                    (project_id, column_id, position,
                     title, assignee_id)
                VALUES
                    (first_project, first_column, 0,
                     'Úkol Jana', first_user),
                    (second_project, second_column, 0,
                     'Úkol Evy', second_user);
            END $$;
            """);

        // Přihlášení Jana.
        var login = await app.Client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "jan.novak@example.test",
                password = "moje-dlouhe-heslo-2026"
            });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await app.Client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var projects = json.GetProperty("projects");
        var tasks = json.GetProperty("tasks");

        // Jan vidí pouze svůj projekt.
        Assert.Equal(1, projects.GetArrayLength());
        Assert.Equal(
            "Projekt Jana",
            projects[0].GetProperty("name").GetString());

        Assert.Equal(
            1,
            projects[0].GetProperty("membersCount").GetInt32());

        Assert.Equal(
            1,
            projects[0].GetProperty("total").GetInt32());

        Assert.Equal(
            1,
            projects[0].GetProperty("openTasks").GetInt32());

        // Jan vidí správný termín projektu.
        Assert.Equal(
            "15. 01. 2027",
            projects[0].GetProperty("milestone").GetString());

        // Jan vidí seznam členů svého projektu.
        var members = projects[0].GetProperty("members");

        Assert.Single(members.EnumerateArray());
        Assert.Equal(
            "Jan Novák",
            members[0].GetString());
            
        // Jan vidí pouze svůj úkol.
        Assert.Equal(1, tasks.GetArrayLength());
        Assert.Equal(
            "Úkol Jana",
            tasks[0].GetProperty("name").GetString());

        Assert.Equal(
            "Projekt Jana",
            tasks[0].GetProperty("project").GetString());
    }
}
