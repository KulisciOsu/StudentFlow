using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Ověřuje API nástěnky přes skutečnou HTTP pipeline, přihlášení a databázi s ukázkovým projektem.
/// </summary>
public class BoardApiTests
{
    private const string Password = "moje-dlouhe-heslo-2026";

    /// <summary>
    /// Ověří, že nepřihlášený uživatel dostane 401.
    /// </summary>
    [Fact]
    public async Task Board_Anonymous_ReturnsUnauthorized()
    {
        await using var app = await CreateAppAsync();

        var response = await app.Client.GetAsync("/api/projects/1/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Ověří, že člen načte nástěnku, přesune svůj úkol a přesun zůstane uložený, ale cizí úkol přesunout nesmí.
    /// </summary>
    [Fact]
    public async Task Board_Member_MovesOnlyOwnTaskAndMoveIsPersisted()
    {
        await using var app = await CreateAppAsync();
        await SignInAsync(app, "clen@example.test", "STUDENT");
        await app.Database.ExecuteAsync("""
            INSERT INTO project_member (project_id, user_id, is_leader)
            SELECT 1, user_id, FALSE FROM app_user WHERE email = 'clen@example.test';
            UPDATE task SET assignee_id = (SELECT user_id FROM app_user WHERE email = 'clen@example.test') WHERE task_id = 4;
            """);

        var project = await GetJsonAsync(app, "/api/projects/1");
        var tasks = await GetJsonAsync(app, "/api/projects/1/tasks");
        var own = await app.Client.PutAsJsonAsync("/api/projects/1/tasks/4/position", new { columnId = 3, position = 0 });
        var foreign = await app.Client.PutAsJsonAsync("/api/projects/1/tasks/2/position", new { columnId = 3, position = 0 });
        var saved = await GetJsonAsync(app, "/api/projects/1/tasks");

        Assert.False(project.GetProperty("isReadOnly").GetBoolean());
        Assert.Equal(10, tasks.GetArrayLength());
        Assert.Equal(new[] { 4 }, tasks.EnumerateArray().Where(task => task.GetProperty("canMove").GetBoolean()).Select(task => task.GetProperty("id").GetInt32()));
        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal(3, FindTask(saved, 4).GetProperty("columnId").GetInt32());
        Assert.Equal(1, FindTask(saved, 2).GetProperty("columnId").GetInt32());
    }

    /// <summary>
    /// Ověří, že vedoucí smí přesunout cizí úkol.
    /// </summary>
    [Fact]
    public async Task Board_Leader_MovesForeignTask()
    {
        await using var app = await CreateAppAsync();
        await SignInAsync(app, "vedouci@example.test", "STUDENT");
        await app.Database.ExecuteAsync("""
            UPDATE project_member SET is_leader = FALSE WHERE project_id = 1;
            INSERT INTO project_member (project_id, user_id, is_leader)
            SELECT 1, user_id, TRUE FROM app_user WHERE email = 'vedouci@example.test';
            """);

        var response = await app.Client.PutAsJsonAsync("/api/projects/1/tasks/2/position", new { columnId = 3, position = 0 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Ověří, že vyučující nástěnku vidí jen pro čtení a přesun mu API odmítne.
    /// </summary>
    [Fact]
    public async Task Board_Teacher_IsReadOnly()
    {
        await using var app = await CreateAppAsync();
        await SignInAsync(app, "ucitel@example.test", "TEACHER");

        var project = await GetJsonAsync(app, "/api/projects/1");
        var tasks = await GetJsonAsync(app, "/api/projects/1/tasks");
        var move = await app.Client.PutAsJsonAsync("/api/projects/1/tasks/2/position", new { columnId = 3, position = 0 });

        Assert.True(project.GetProperty("isReadOnly").GetBoolean());
        Assert.All(tasks.EnumerateArray(), task => Assert.False(task.GetProperty("canMove").GetBoolean()));
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
    }

    /// <summary>
    /// Ověří, že student mimo projekt nástěnku nenačte ani nezmění.
    /// </summary>
    [Fact]
    public async Task Board_Outsider_ReturnsForbidden()
    {
        await using var app = await CreateAppAsync();
        await SignInAsync(app, "mimo@example.test", "STUDENT");

        var project = await app.Client.GetAsync("/api/projects/1");
        var columns = await app.Client.GetAsync("/api/projects/1/columns");
        var tasks = await app.Client.GetAsync("/api/projects/1/tasks");
        var move = await app.Client.PutAsJsonAsync("/api/projects/1/tasks/2/position", new { columnId = 3, position = 0 });

        Assert.Equal(HttpStatusCode.Forbidden, project.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, columns.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, tasks.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, move.StatusCode);
    }

    /// <summary>
    /// Vytvoří API nad databází s ukázkovým projektem a jeho členy.
    /// </summary>
    private static async Task<RegistrationApp> CreateAppAsync()
    {
        var app = await RegistrationApp.CreateAsync(dashboard: true);

        try
        {
            await app.Database.ApplyMigrationAsync("V005__seed_kanban_demo.sql");
            await app.Database.ApplyMigrationAsync("V20261110_1832__seed_kanban_demo_members.sql");

            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Zaregistruje a přihlásí uživatele.
    /// </summary>
    private static async Task SignInAsync(RegistrationApp app, string email, string accountType)
    {
        await app.SetCsrfAsync();

        var registration = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(email, accountType, Password));
        var login = await app.Client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    /// <summary>
    /// Načte JSON odpověď a ověří stav 200.
    /// </summary>
    private static async Task<JsonElement> GetJsonAsync(RegistrationApp app, string url)
    {
        var response = await app.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>
    /// Najde úkol v JSON poli podle identifikátoru.
    /// </summary>
    private static JsonElement FindTask(JsonElement tasks, int id)
    {
        return tasks.EnumerateArray().Single(task => task.GetProperty("id").GetInt32() == id);
    }
}
