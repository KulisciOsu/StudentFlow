
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
    /// Přihlášený uživatel získá projekty a úkoly dashboardu.
    /// </summary>
    [Fact]
    public async Task GetDashboard_AuthenticatedUser_Returns200WithData()
    {
        await using var app = await RegistrationApp.CreateAsync();

        await app.SetCsrfAsync();

        var registration = await app.Client.PostAsJsonAsync(
            "/api/auth/register",
            RegistrationApp.Valid()
        );
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await app.Client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "jan.novak@example.test",
                password = "moje-dlouhe-heslo-2026"
            }
        );
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var response = await app.Client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.GetProperty("projects").GetArrayLength() > 0);
        Assert.True(json.GetProperty("tasks").GetArrayLength() > 0);
    }
}
