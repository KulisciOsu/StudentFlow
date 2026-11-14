using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Integrační testy API členů projektu proti PostgreSQL s migracemi V001–V004, V006 a migrací správy členů.
/// </summary>
/// <remarks>Tým „Tým Sova“: vedoucí Petra a člen Jan; Lucie (studentka) a Eva (vyučující) do týmu nepatří.</remarks>
public class ProjectMembersApiTests
{
    private const string Migration = "V20261114_2156__project_member_management.sql";
    private const string Password = "moje-dlouhe-heslo-2026";
    private const string MembersPath = "/api/projects/1/members";
    private const string MissingProjectMembersPath = "/api/projects/999/members";

    /// <summary>
    /// Nepřihlášený uživatel seznam nedostane.
    /// </summary>
    [Fact]
    public async Task List_Anonymous_Returns401()
    {
        await using var app = await CreateTeamAsync();

        var response = await app.CreateClient().GetAsync(MembersPath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("UNAUTHENTICATED", (await ProblemAsync(response)).GetProperty("code").GetString());
    }

    /// <summary>
    /// Vedoucí i běžný člen dostanou název týmu a členy, vedoucí první; odpověď se neukládá do mezipaměti.
    /// </summary>
    [Theory]
    [InlineData("petra@example.test")]
    [InlineData("jan@example.test")]
    public async Task List_AnyMember_ReturnsTeamNameAndMembersLeaderFirst(string email)
    {
        await using var app = await CreateTeamAsync();
        var client = await SignInAsync(app, email);

        var response = await client.GetAsync(MembersPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tým Sova", json.GetProperty("teamName").GetString());
        var members = json.GetProperty("members").EnumerateArray().ToList();
        Assert.Equal(new[] { "Petra Svobodová", "Jan Novák" }, members.Select(m => m.GetProperty("displayName").GetString()));
        Assert.Equal(new[] { true, false }, members.Select(m => m.GetProperty("isLeader").GetBoolean()));
        Assert.Equal(new[] { "petra@example.test", "jan@example.test" }, members.Select(m => m.GetProperty("email").GetString()));
    }

    /// <summary>
    /// Člen má právě pět veřejných polí; ID uživatele je text a čas vstupu je čitelné datum.
    /// </summary>
    [Fact]
    public async Task List_Member_HasOnlyPublicFieldsWithStringUserId()
    {
        await using var app = await CreateTeamAsync();
        var client = await SignInAsync(app, "jan@example.test");

        var json = await (await client.GetAsync(MembersPath)).Content.ReadFromJsonAsync<JsonElement>();

        var jan = json.GetProperty("members")[1];
        Assert.Equal(new[] { "userId", "displayName", "email", "isLeader", "joinedAt" }, jan.EnumerateObject().Select(p => p.Name));
        Assert.Equal(JsonValueKind.String, jan.GetProperty("userId").ValueKind);
        Assert.Equal(await app.Database.ScalarAsync<long>("SELECT user_id FROM app_user WHERE email = 'jan@example.test'"),
            long.Parse(jan.GetProperty("userId").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(jan.GetProperty("joinedAt").TryGetDateTimeOffset(out _));
    }

    /// <summary>
    /// Nečlen, studentka i vyučující, dostane přesně stejnou odpověď jako u neexistujícího projektu.
    /// </summary>
    [Theory]
    [InlineData("lucie@example.test")]
    [InlineData("eva@example.test")]
    public async Task List_NonMember_Returns404SameAsMissingProject(string email)
    {
        await using var app = await CreateTeamAsync();
        var client = await SignInAsync(app, email);

        var foreign = await client.GetAsync(MembersPath);
        var missing = await client.GetAsync(MissingProjectMembersPath);

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var foreignProblem = await ProblemAsync(foreign);
        var missingProblem = await ProblemAsync(missing);
        Assert.Equal("PROJECT_NOT_FOUND", foreignProblem.GetProperty("code").GetString());
        foreach (var field in new[] { "code", "title", "detail", "status" })
            Assert.Equal(missingProblem.GetProperty(field).ToString(), foreignProblem.GetProperty(field).ToString());
        Assert.False(foreignProblem.TryGetProperty("teamName", out _));
    }

    /// <summary>
    /// Členství se ověřuje při každém požadavku: po odebrání v databázi dostane stejná session hned 404.
    /// </summary>
    [Fact]
    public async Task List_MemberRemovedMeanwhile_Returns404OnNextRequest()
    {
        await using var app = await CreateTeamAsync();
        var client = await SignInAsync(app, "jan@example.test");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(MembersPath)).StatusCode);

        await app.Database.ExecuteAsync("""
            DELETE FROM project_member
            WHERE user_id = (SELECT user_id FROM app_user WHERE email = 'jan@example.test')
            """);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(MembersPath)).StatusCode);
    }

    /// <summary>
    /// Připraví API s migracemi, účty Petry, Jana, Lucie a Evy a projektem „Tým Sova“ s Petrou jako vedoucí a Janem jako členem.
    /// </summary>
    private static async Task<RegistrationApp> CreateTeamAsync()
    {
        var app = await RegistrationApp.CreateAsync(dashboard: true);

        try
        {
            await app.Database.ApplyMigrationAsync(Migration);
            await RegisterAsync(app, "petra@example.test", "Petra Svobodová");
            await RegisterAsync(app, "jan@example.test", "Jan Novák");
            await RegisterAsync(app, "lucie@example.test", "Lucie Černá");
            await RegisterAsync(app, "eva@example.test", "Eva Horáková", "TEACHER");
            await app.Database.ExecuteAsync("""
                INSERT INTO project (name, team_name) VALUES ('Rezervační systém knihovny', 'Tým Sova');
                INSERT INTO project_member (project_id, user_id, is_leader)
                SELECT 1, user_id, email = 'petra@example.test'
                FROM app_user
                WHERE email IN ('petra@example.test', 'jan@example.test');
                """);
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Zaregistruje účet přes API, stejně jako by to udělal uživatel.
    /// </summary>
    private static async Task RegisterAsync(RegistrationApp app, string email, string displayName, string accountType = "STUDENT")
    {
        var client = app.CreateClient();
        await RegistrationApp.SetCsrfAsync(client);
        var payload = RegistrationApp.Valid(email, accountType, Password);
        payload["displayName"] = displayName;

        var response = await client.PostAsJsonAsync("/api/auth/register", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Přihlásí účet ve vlastním klientovi s vlastními cookies a připraví CSRF token pro jeho identitu.
    /// </summary>
    private static async Task<HttpClient> SignInAsync(RegistrationApp app, string email)
    {
        var client = app.CreateClient();
        await RegistrationApp.SetCsrfAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await RegistrationApp.SetCsrfAsync(client);
        return client;
    }

    /// <summary>
    /// Přečte tělo chybové odpovědi ve formátu ProblemDetails.
    /// </summary>
    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}
