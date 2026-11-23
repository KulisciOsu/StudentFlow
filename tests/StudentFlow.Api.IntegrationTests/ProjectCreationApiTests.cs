
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
    /// Požadavek bez CSRF tokenu vrátí 400 a žádný projekt nevytvoří.
    /// </summary>
    [Fact]
    public async Task Create_WithoutCsrfToken_Returns400AndDoesNotWrite()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "bez-tokenu@example.test", "STUDENT");
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var response = await app.Client.PostAsJsonAsync("/api/projects", ValidProject());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = await app.Database.ScalarAsync<long>("SELECT count(*) FROM project");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("CSRF_INVALID", problem.GetProperty("code").GetString());
        Assert.Equal(0L, count);
    }

    /// <summary>
    /// Neplatný CSRF token má stejnou odpověď jako chybějící token.
    /// </summary>
    [Fact]
    public async Task Create_InvalidCsrfToken_Returns400AndDoesNotWrite()
    {
        await using var app = await RegistrationApp.CreateAsync(dashboard: true);
        await SignInAsync(app, "spatny-token@example.test", "STUDENT");
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        app.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid-token");

        var response = await app.Client.PostAsJsonAsync("/api/projects", ValidProject());
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = await app.Database.ScalarAsync<long>("SELECT count(*) FROM project");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("CSRF_INVALID", problem.GetProperty("code").GetString());
        Assert.Equal(0L, count);
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
