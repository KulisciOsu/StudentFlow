using System.Net;
using System.Net.Http.Json;
using System.Text;
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
    /// Vedoucí přidá studentku podle e-mailu s mezerami a velkými písmeny; vznikne běžné členství s autorem přidání.
    /// </summary>
    [Fact]
    public async Task Add_LeaderAddsStudent_Returns201AndMemberAppearsInList()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var response = await petra.PostAsJsonAsync(MembersPath, new { email = "  LUCIE@Example.TEST " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var member = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("member");
        Assert.Equal("Lucie Černá", member.GetProperty("displayName").GetString());
        Assert.Equal("lucie@example.test", member.GetProperty("email").GetString());
        Assert.False(member.GetProperty("isLeader").GetBoolean());
        var list = await (await petra.GetAsync(MembersPath)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "Petra Svobodová", "Jan Novák", "Lucie Černá" },
            list.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("displayName").GetString()));
        Assert.Equal("petra@example.test", await app.Database.ScalarAsync<string>("""
            SELECT adder.email
            FROM project_member pm
            JOIN app_user member ON member.user_id = pm.user_id
            JOIN app_user adder ON adder.user_id = pm.added_by
            WHERE member.email = 'lucie@example.test'
            """));
    }

    /// <summary>
    /// Neplatný e-mail, neexistující účet, vyučující a stávající člen skončí domluvenou chybou a nikdo nepřibude.
    /// </summary>
    [Theory]
    [InlineData("", HttpStatusCode.BadRequest, "VALIDATION_FAILED")]
    [InlineData("lucie", HttpStatusCode.BadRequest, "VALIDATION_FAILED")]
    [InlineData("nikdo@example.test", HttpStatusCode.UnprocessableEntity, "ACCOUNT_NOT_FOUND")]
    [InlineData("eva@example.test", HttpStatusCode.UnprocessableEntity, "ACCOUNT_NOT_STUDENT")]
    [InlineData("jan@example.test", HttpStatusCode.Conflict, "MEMBER_ALREADY_EXISTS")]
    [InlineData("petra@example.test", HttpStatusCode.Conflict, "MEMBER_ALREADY_EXISTS")]
    public async Task Add_LeaderWithRejectedEmail_ReturnsErrorAndAddsNobody(string email, HttpStatusCode status, string code)
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var response = await petra.PostAsJsonAsync(MembersPath, new { email });

        var problem = await AssertProblemAsync(response, status, code);
        if (status == HttpStatusCode.BadRequest)
            Assert.True(problem.GetProperty("errors").TryGetProperty("email", out _));
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Neaktivní účet se hlásí stejně jako neexistující.
    /// </summary>
    [Fact]
    public async Task Add_InactiveAccount_Returns422AccountNotFound()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");
        await app.Database.ExecuteAsync("UPDATE app_user SET is_active = FALSE WHERE email = 'lucie@example.test'");

        var response = await petra.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test" });

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "ACCOUNT_NOT_FOUND");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Běžný člen nesmí přidávat; dostane 403, protože projekt vidí.
    /// </summary>
    [Fact]
    public async Task Add_RegularMember_Returns403LeaderRequired()
    {
        await using var app = await CreateTeamAsync();
        var jan = await SignInAsync(app, "jan@example.test");

        var response = await jan.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "PROJECT_LEADER_REQUIRED");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Nečlen nepřidá nikoho ani sám sebe a dostane stejnou 404 jako u neexistujícího projektu.
    /// </summary>
    [Fact]
    public async Task Add_NonMember_Returns404SameAsMissingProject()
    {
        await using var app = await CreateTeamAsync();
        var lucie = await SignInAsync(app, "lucie@example.test");

        var foreign = await lucie.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test" });
        var missing = await lucie.PostAsJsonAsync(MissingProjectMembersPath, new { email = "lucie@example.test" });

        await AssertProblemAsync(foreign, HttpStatusCode.NotFound, "PROJECT_NOT_FOUND");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "PROJECT_NOT_FOUND");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Bez CSRF tokenu se nic nepřidá, i když požadavek posílá přihlášená vedoucí.
    /// </summary>
    [Fact]
    public async Task Add_WithoutCsrfToken_Returns403CsrfInvalid()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");
        petra.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var response = await petra.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test" });

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "CSRF_INVALID");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Nepřihlášený uživatel nepřidá nikoho ani s platným CSRF tokenem.
    /// </summary>
    [Fact]
    public async Task Add_Anonymous_Returns401()
    {
        await using var app = await CreateTeamAsync();
        var anonymous = app.CreateClient();
        await RegistrationApp.SetCsrfAsync(anonymous);

        var response = await anonymous.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test" });

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Neznámé pole odmítne API, takže klient si nemůže sám nastavit vedení.
    /// </summary>
    [Fact]
    public async Task Add_ExtraField_Returns400AndAddsNobody()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var response = await petra.PostAsJsonAsync(MembersPath, new { email = "lucie@example.test", isLeader = true });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Jiný formát než JSON a příliš velké tělo odmítne stejná ochrana jako u registrace.
    /// </summary>
    [Fact]
    public async Task Add_NonJsonOrTooLargeBody_Returns415Or413()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var form = await petra.PostAsync(MembersPath,
            new StringContent("email=lucie@example.test", Encoding.UTF8, "application/x-www-form-urlencoded"));
        var tooLarge = await petra.PostAsJsonAsync(MembersPath, new { email = new string('a', 17 * 1024) + "@example.test" });

        await AssertProblemAsync(form, HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE");
        await AssertProblemAsync(tooLarge, HttpStatusCode.RequestEntityTooLarge, "PAYLOAD_TOO_LARGE");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Vedoucí odebere Jana: Jan hned ztratí přístup, seznam ho neobsahuje a druhé odebrání skončí 404.
    /// </summary>
    [Fact]
    public async Task Remove_LeaderRemovesMember_Returns204AndMemberLosesAccess()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");
        var jan = await SignInAsync(app, "jan@example.test");
        var janPath = await MemberPathAsync(app, "jan@example.test");

        var response = await petra.DeleteAsync(janPath);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertProblemAsync(await jan.GetAsync(MembersPath), HttpStatusCode.NotFound, "PROJECT_NOT_FOUND");
        var list = await (await petra.GetAsync(MembersPath)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "Petra Svobodová" },
            list.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("displayName").GetString()));
        await AssertProblemAsync(await petra.DeleteAsync(janPath), HttpStatusCode.NotFound, "MEMBER_NOT_FOUND");
    }

    /// <summary>
    /// Vedoucí nemůže odebrat sám sebe; projekt musí mít vždy vedoucího.
    /// </summary>
    [Fact]
    public async Task Remove_Leader_Returns409AndLeaderStays()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var response = await petra.DeleteAsync(await MemberPathAsync(app, "petra@example.test"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "LEADER_CANNOT_BE_REMOVED");
        Assert.True(await app.Database.ScalarAsync<bool>("""
            SELECT is_leader FROM project_member JOIN app_user USING (user_id) WHERE email = 'petra@example.test'
            """));
    }

    /// <summary>
    /// Odebrání uživatele, který v týmu není, nebo neexistujícího ID skončí 404 MEMBER_NOT_FOUND.
    /// </summary>
    [Fact]
    public async Task Remove_UserWhoIsNotMember_Returns404MemberNotFound()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");

        var lucie = await petra.DeleteAsync(await MemberPathAsync(app, "lucie@example.test"));
        var unknown = await petra.DeleteAsync($"{MembersPath}/987654321");

        await AssertProblemAsync(lucie, HttpStatusCode.NotFound, "MEMBER_NOT_FOUND");
        await AssertProblemAsync(unknown, HttpStatusCode.NotFound, "MEMBER_NOT_FOUND");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Běžný člen nesmí odebrat vedoucí ani sám sebe.
    /// </summary>
    [Theory]
    [InlineData("petra@example.test")]
    [InlineData("jan@example.test")]
    public async Task Remove_RegularMember_Returns403LeaderRequired(string targetEmail)
    {
        await using var app = await CreateTeamAsync();
        var jan = await SignInAsync(app, "jan@example.test");

        var response = await jan.DeleteAsync(await MemberPathAsync(app, targetEmail));

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "PROJECT_LEADER_REQUIRED");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Nečlen nikoho neodebere a dostane stejnou 404 jako u neexistujícího projektu.
    /// </summary>
    [Fact]
    public async Task Remove_NonMember_Returns404SameAsMissingProject()
    {
        await using var app = await CreateTeamAsync();
        var lucie = await SignInAsync(app, "lucie@example.test");
        var janId = await UserIdAsync(app, "jan@example.test");

        var foreign = await lucie.DeleteAsync($"{MembersPath}/{janId}");
        var missing = await lucie.DeleteAsync($"{MissingProjectMembersPath}/{janId}");

        await AssertProblemAsync(foreign, HttpStatusCode.NotFound, "PROJECT_NOT_FOUND");
        await AssertProblemAsync(missing, HttpStatusCode.NotFound, "PROJECT_NOT_FOUND");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Bez CSRF tokenu se nikdo neodebere, i když požadavek posílá přihlášená vedoucí.
    /// </summary>
    [Fact]
    public async Task Remove_WithoutCsrfToken_Returns403CsrfInvalid()
    {
        await using var app = await CreateTeamAsync();
        var petra = await SignInAsync(app, "petra@example.test");
        petra.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");

        var response = await petra.DeleteAsync(await MemberPathAsync(app, "jan@example.test"));

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "CSRF_INVALID");
        Assert.Equal(2L, await CountMembersAsync(app));
    }

    /// <summary>
    /// Nepřihlášený uživatel nikoho neodebere.
    /// </summary>
    [Fact]
    public async Task Remove_Anonymous_Returns401()
    {
        await using var app = await CreateTeamAsync();
        var anonymous = app.CreateClient();
        await RegistrationApp.SetCsrfAsync(anonymous);

        var response = await anonymous.DeleteAsync(await MemberPathAsync(app, "jan@example.test"));

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "UNAUTHENTICATED");
        Assert.Equal(2L, await CountMembersAsync(app));
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

    /// <summary>
    /// Ověří HTTP status a kód chyby a vrátí tělo ProblemDetails pro další kontroly.
    /// </summary>
    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await ProblemAsync(response);
        Assert.Equal(code, problem.GetProperty("code").GetString());
        return problem;
    }

    /// <summary>
    /// ID účtu podle e-mailu.
    /// </summary>
    private static Task<long> UserIdAsync(RegistrationApp app, string email) =>
        app.Database.ScalarAsync<long>($"SELECT user_id FROM app_user WHERE email = '{email}'");

    /// <summary>
    /// Adresa člena v testovacím projektu podle e-mailu jeho účtu.
    /// </summary>
    private static async Task<string> MemberPathAsync(RegistrationApp app, string email) =>
        $"{MembersPath}/{await UserIdAsync(app, email)}";

    /// <summary>
    /// Počet všech členství v databázi; tým má na začátku dva členy.
    /// </summary>
    private static Task<long> CountMembersAsync(RegistrationApp app) =>
        app.Database.ScalarAsync<long>("SELECT count(*) FROM project_member");
}
