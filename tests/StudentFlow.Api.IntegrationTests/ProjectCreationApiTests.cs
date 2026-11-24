
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Ověřuje bezpečnost a serverová pravidla vytvoření projektu přes skutečné HTTP API a PostgreSQL.
/// </summary>
public class ProjectCreationApiTests
{
    private const string Password = "TestHeslo1!";

    /// <summary>
    /// Požadavek bez CSRF tokenu vrátí 403 a žádný projekt nevytvoří.
    /// </summary>
    [Fact]
    public async Task Create_WithoutCsrfToken_Returns403AndDoesNotWrite()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "bez-tokenu@example.test", "STUDENT");
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var response = await app.Client.PostAsJsonAsync("/api/projects", ValidProject());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = await app.Database.ScalarAsync<long>("SELECT count(*) FROM project");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("CSRF_INVALID", problem.GetProperty("code").GetString());
        Assert.Equal(0L, count);
    }

    /// <summary>
    /// Neplatný CSRF token má stejnou odpověď jako chybějící token.
    /// </summary>
    [Fact]
    public async Task Create_InvalidCsrfToken_Returns403AndDoesNotWrite()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "spatny-token@example.test", "STUDENT");
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        app.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");

        var response = await app.Client.PostAsJsonAsync("/api/projects", ValidProject());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = await app.Database.ScalarAsync<long>("SELECT count(*) FROM project");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("CSRF_INVALID", problem.GetProperty("code").GetString());
        Assert.Equal(0L, count);
    }

    /// <summary>
    /// Vyučující s platným CSRF tokenem dostane 403 a projekt nevznikne.
    /// </summary>
    [Fact]
    public async Task Create_Teacher_Returns403AndDoesNotWrite()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "vyucujici@example.test", "TEACHER");

        var response = await app.Client.PostAsJsonAsync("/api/projects", ValidProject());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = await app.Database.ScalarAsync<long>("SELECT count(*) FROM project");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("PROJECT_CREATION_FORBIDDEN", problem.GetProperty("code").GetString());
        Assert.Equal("Nový projekt může založit pouze student.", problem.GetProperty("detail").GetString());
        Assert.Equal(0L, count);
    }
    /// <summary>
    /// Student smí založit projekt s maximálními délkami a bez termínu.
    /// </summary>
    [Fact]
    public async Task Create_Student_AcceptsServerLimitsAndOptionalDate()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "student@example.test", "STUDENT");
        var command = new
        {
            name = new string('P', 200),
            description = "",
            teamName = new string('T', 120),
            dueDate = (DateOnly?)null
        };

        var response = await app.Client.PostAsJsonAsync("/api/projects", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var projectId = result.GetProperty("projectId").GetInt32();
        var count = await app.Database.ScalarAsync<long>($"""
            SELECT count(*) FROM project
            WHERE project_id = {projectId}
              AND char_length(name) = 200
              AND char_length(team_name) = 120
              AND due_date IS NULL
            """);

        Assert.Equal(1L, count);
    }
    /// <summary>
    /// Zaregistruje a přihlásí účet a vydá CSRF token pro novou session.
    /// </summary>
    private static async Task SignInAsync(RegistrationApp app, string email, string accountType)
    {
        await app.SetCsrfAsync();
        var registration = await app.Client.PostAsJsonAsync(
            "/api/auth/register", RegistrationApp.Valid(email, accountType, Password));
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await app.Client.PostAsJsonAsync(
            "/api/auth/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        await app.SetCsrfAsync();
    }

    private static object ValidProject() => new
    {
        name = "Testovací projekt",
        description = "",
        teamName = "Testovací tým",
        dueDate = (DateOnly?)null
    };
}
