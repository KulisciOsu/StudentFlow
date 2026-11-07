
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

    /// <summary>
    /// Ověří výpočet statistik projektu s otevřenými,
    /// dokončenými a zpožděnými úkoly.
    /// </summary>
    [Fact]
    public async Task GetDashboard_CalculatesProjectStatistics()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        var registration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid());

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        await app.Database.ExecuteAsync("""
            DO $$
            DECLARE
                v_user_id BIGINT;
                current_project INTEGER;
                todo_column INTEGER;
                done_column INTEGER;
            BEGIN
                SELECT user_id INTO v_user_id
                FROM app_user
                WHERE email = 'jan.novak@example.test';

                INSERT INTO project (name, team_name)
                VALUES ('Statistiky projektu', 'Testovací tým')
                RETURNING project_id INTO current_project;

                INSERT INTO project_member
                    (project_id, user_id, is_leader)
                VALUES (current_project, v_user_id, TRUE);

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (current_project, 'K řešení', 0, FALSE)
                RETURNING column_id INTO todo_column;

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (current_project, 'Hotovo', 1, TRUE)
                RETURNING column_id INTO done_column;

                INSERT INTO task
                    (project_id, column_id, position,
                     title, assignee_id, due_date)
                VALUES
                    (current_project, todo_column, 0,
                     'Otevřený úkol', v_user_id,
                     CURRENT_DATE + 7),
                    (current_project, todo_column, 1,
                     'Zpožděný úkol', v_user_id,
                     CURRENT_DATE - 1),
                    (current_project, done_column, 0,
                     'Dokončený úkol', v_user_id,
                     CURRENT_DATE - 5);
            END $$;
            """);

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

        var project = json.GetProperty("projects")[0];
        var tasks = json.GetProperty("tasks");

        Assert.Equal(3, project.GetProperty("total").GetInt32());
        Assert.Equal(1, project.GetProperty("completed").GetInt32());
        Assert.Equal(2, project.GetProperty("openTasks").GetInt32());
        Assert.Equal(1, project.GetProperty("overdueTasks").GetInt32());

        Assert.Equal(3, tasks.GetArrayLength());

        var overdueTask = tasks.EnumerateArray()
            .Single(t => t.GetProperty("name").GetString()
                == "Zpožděný úkol");

        Assert.True(overdueTask.GetProperty("overdue").GetBoolean());
        Assert.False(overdueTask.GetProperty("completed").GetBoolean());

        var completedTask = tasks.EnumerateArray()
            .Single(t => t.GetProperty("name").GetString()
                == "Dokončený úkol");

        Assert.True(completedTask.GetProperty("completed").GetBoolean());
        Assert.False(completedTask.GetProperty("overdue").GetBoolean());
    }

    /// <summary>
    /// Statistiky projektu zahrnují všechny úkoly,
    /// osobní statistiky pouze úkoly přihlášeného uživatele.
    /// </summary>
    [Fact]
    public async Task GetDashboard_SharedProject_SeparatesUserStatistics()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        var janRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("jan.novak@example.test"));

        Assert.Equal(HttpStatusCode.Created, janRegistration.StatusCode);

        var evaRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("eva.svobodova@example.test"));

        Assert.Equal(HttpStatusCode.Created, evaRegistration.StatusCode);

        await app.Database.ExecuteAsync("""
            DO $$
            DECLARE
                v_jan_id BIGINT;
                v_eva_id BIGINT;
                v_project_id INTEGER;
                v_todo_id INTEGER;
                v_done_id INTEGER;
            BEGIN
                SELECT user_id INTO v_jan_id
                FROM app_user
                WHERE email = 'jan.novak@example.test';

                SELECT user_id INTO v_eva_id
                FROM app_user
                WHERE email = 'eva.svobodova@example.test';

                INSERT INTO project (name, team_name)
                VALUES ('Společný projekt', 'Tým A')
                RETURNING project_id INTO v_project_id;

                INSERT INTO project_member
                    (project_id, user_id, is_leader)
                VALUES
                    (v_project_id, v_jan_id, TRUE),
                    (v_project_id, v_eva_id, FALSE);

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (v_project_id, 'K řešení', 0, FALSE)
                RETURNING column_id INTO v_todo_id;

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (v_project_id, 'Hotovo', 1, TRUE)
                RETURNING column_id INTO v_done_id;

                INSERT INTO task
                    (project_id, column_id, position,
                     title, assignee_id, due_date)
                VALUES
                    (v_project_id, v_todo_id, 0,
                     'Janův úkol', v_jan_id, CURRENT_DATE + 5),
                    (v_project_id, v_todo_id, 1,
                     'Evin zpožděný úkol', v_eva_id, CURRENT_DATE - 2),
                    (v_project_id, v_done_id, 0,
                     'Evin dokončený úkol', v_eva_id, CURRENT_DATE - 3);
            END $$;
            """);

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

        Assert.Equal(1, projects.GetArrayLength());

        var project = projects[0];

        Assert.Equal(
            "Společný projekt",
            project.GetProperty("name").GetString());

        Assert.Equal(
            "Vedoucí",
            project.GetProperty("role").GetString());

        Assert.Equal(2, project.GetProperty("membersCount").GetInt32());
        Assert.Equal(2, project.GetProperty("members").GetArrayLength());

        // Statistiky celého projektu.
        Assert.Equal(3, project.GetProperty("total").GetInt32());
        Assert.Equal(1, project.GetProperty("completed").GetInt32());

        // Osobní statistiky Jana.
        Assert.Equal(1, project.GetProperty("openTasks").GetInt32());
        Assert.Equal(0, project.GetProperty("overdueTasks").GetInt32());

        // Eva má dva úkoly, ale Jan vidí pouze svůj.
        Assert.Equal(1, tasks.GetArrayLength());

        Assert.Equal(
            "Janův úkol",
            tasks[0].GetProperty("name").GetString());
    }

    /// <summary>
    /// Ověří více projektů, prázdný projekt
    /// a oddělené statistiky jednotlivých projektů.
    /// </summary>
    [Fact]
    public async Task GetDashboard_MultipleProjects_ReturnsCorrectStatistics()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        var registration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid());

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        await app.Database.ExecuteAsync("""
            DO $$
            DECLARE
                v_user_id BIGINT;
                v_empty_project INTEGER;
                v_active_project INTEGER;
                v_todo_column INTEGER;
                v_done_column INTEGER;
            BEGIN
                SELECT user_id INTO v_user_id
                FROM app_user
                WHERE email = 'jan.novak@example.test';

                INSERT INTO project (name, team_name)
                VALUES ('Prázdný projekt', 'Tým A')
                RETURNING project_id INTO v_empty_project;

                INSERT INTO project (name, team_name)
                VALUES ('Aktivní projekt', 'Tým B')
                RETURNING project_id INTO v_active_project;

                INSERT INTO project_member
                    (project_id, user_id, is_leader)
                VALUES
                    (v_empty_project, v_user_id, TRUE),
                    (v_active_project, v_user_id, TRUE);

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (v_active_project, 'K řešení', 0, FALSE)
                RETURNING column_id INTO v_todo_column;

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (v_active_project, 'Hotovo', 1, TRUE)
                RETURNING column_id INTO v_done_column;

                INSERT INTO task
                    (project_id, column_id, position,
                     title, assignee_id, due_date)
                VALUES
                    (v_active_project, v_todo_column, 0,
                     'Aktivní úkol', v_user_id, CURRENT_DATE - 1),
                    (v_active_project, v_done_column, 0,
                     'Hotový úkol', v_user_id, CURRENT_DATE - 2);
            END $$;
            """);

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

        Assert.Equal(2, projects.GetArrayLength());
        Assert.Equal(2, tasks.GetArrayLength());

        var emptyProject = projects.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString()
                == "Prázdný projekt");

        Assert.Equal(0, emptyProject.GetProperty("total").GetInt32());
        Assert.Equal(0, emptyProject.GetProperty("completed").GetInt32());
        Assert.Equal(0, emptyProject.GetProperty("openTasks").GetInt32());
        Assert.Equal(0, emptyProject.GetProperty("overdueTasks").GetInt32());

        var activeProject = projects.EnumerateArray()
            .Single(p => p.GetProperty("name").GetString()
                == "Aktivní projekt");

        Assert.Equal(2, activeProject.GetProperty("total").GetInt32());
        Assert.Equal(1, activeProject.GetProperty("completed").GetInt32());
        Assert.Equal(1, activeProject.GetProperty("openTasks").GetInt32());
        Assert.Equal(1, activeProject.GetProperty("overdueTasks").GetInt32());

        Assert.All(
            tasks.EnumerateArray(),
            task => Assert.Equal(
                "Aktivní projekt",
                task.GetProperty("project").GetString()));
    }

    /// <summary>
    /// Přiřazení úkolu samo o sobě nezakládá přístup
    /// k projektu, jehož uživatel není členem.
    /// </summary>
    [Fact]
    public async Task GetDashboard_NonMemberAssignedTask_IsNotVisible()
    {
        await using var app =
            await RegistrationApp.CreateAsync(dashboard: true);

        await app.SetCsrfAsync();

        var janRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("jan.novak@example.test"));

        Assert.Equal(HttpStatusCode.Created, janRegistration.StatusCode);

        var evaRegistration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid("eva.svobodova@example.test"));

        Assert.Equal(HttpStatusCode.Created, evaRegistration.StatusCode);

        await app.Database.ExecuteAsync("""
            DO $$
            DECLARE
                v_jan_id BIGINT;
                v_eva_id BIGINT;
                v_project_id INTEGER;
                v_column_id INTEGER;
            BEGIN
                SELECT user_id INTO v_jan_id
                FROM app_user
                WHERE email = 'jan.novak@example.test';

                SELECT user_id INTO v_eva_id
                FROM app_user
                WHERE email = 'eva.svobodova@example.test';

                INSERT INTO project (name, team_name)
                VALUES ('Soukromý projekt Evy', 'Tým B')
                RETURNING project_id INTO v_project_id;

                INSERT INTO project_member
                    (project_id, user_id, is_leader)
                VALUES (v_project_id, v_eva_id, TRUE);

                INSERT INTO board_column
                    (project_id, title, position, is_done)
                VALUES (v_project_id, 'K řešení', 0, FALSE)
                RETURNING column_id INTO v_column_id;

                -- Jan je řešitelem, ale není členem projektu.
                INSERT INTO task
                    (project_id, column_id, position,
                     title, assignee_id)
                VALUES
                    (v_project_id, v_column_id, 0,
                     'Nepřístupný úkol', v_jan_id);
            END $$;
            """);

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

        Assert.Equal(
            0,
            json.GetProperty("projects").GetArrayLength());

        Assert.Equal(
            0,
            json.GetProperty("tasks").GetArrayLength());
    }

}
