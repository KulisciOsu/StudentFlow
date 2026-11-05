using System.Buffers.Binary;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Api.IntegrationTests;

public class SessionTests
{
    private const string Password = "moje-dlouhe-heslo-2026";
    private const string CookieName = "__Host-StudentFlow.Session";
    private static object Login(string email = "jan.novak@example.test", string password = Password, bool rememberMe = false) => new { email, password, rememberMe };
    private static async Task SeedAsync(RegistrationApp app, string type = "STUDENT")
    {
        await app.SetCsrfAsync();
        Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(type: type))).StatusCode);
    }
    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, bool rememberMe = false)
    {
        await RegistrationApp.SetCsrfAsync(client);
        return await client.PostAsJsonAsync("/api/auth/login", Login(rememberMe: rememberMe));
    }
    private static string Token(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie")
        .Single(value => value.StartsWith(CookieName + "=")).Split(';')[0][(CookieName.Length + 1)..];
    private static HttpClient Replay(RegistrationApp app, string token)
    {
        var client = app.CreateClient(cookies: false);
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieName}={token}");
        return client;
    }
    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Null(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(Password, body.GetRawText());
        Assert.DoesNotContain("sensitive-", body.GetRawText());
        return body;
    }

    [Theory]
    [InlineData("STUDENT", false)]
    [InlineData("STUDENT", true)]
    [InlineData("TEACHER", false)]
    [InlineData("TEACHER", true)]
    public async Task Login_SetsSecureOpaqueCookieAndStoresOnlyItsHash(string type, bool persistent)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await SeedAsync(app, type);
        var response = await LoginAsync(app.Client, persistent);
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.EnumerateObject().Count());
        Assert.Equal(type, json.GetProperty("user").GetProperty("accountType").GetString());
        Assert.Equal(4, json.GetProperty("user").EnumerateObject().Count());
        var expires = json.GetProperty("session").GetProperty("expiresAt").GetDateTimeOffset();
        Assert.InRange((expires - app.Clock.GetUtcNow()).TotalSeconds, (persistent ? 720 : 8) * 3600 - .01, (persistent ? 720 : 8) * 3600 + .01);
        Assert.Equal(persistent, json.GetProperty("session").GetProperty("persistent").GetBoolean());
        var cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith(CookieName + "="));
        Assert.Contains("secure", cookie.ToLowerInvariant()); Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookie.ToLowerInvariant()); Assert.Contains("path=/", cookie.ToLowerInvariant());
        Assert.DoesNotContain("domain=", cookie.ToLowerInvariant());
        Assert.Equal(persistent, cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(persistent, cookie.Contains("max-age=2592000", StringComparison.OrdinalIgnoreCase));
        var token = Token(response);
        Assert.Equal(43, token.Length);
        var hash = SHA256.HashData(WebEncoders.Base64UrlDecode(token));
        Assert.Equal(hash, await app.Database.ScalarAsync<byte[]>("SELECT token_hash FROM user_session"));
        Assert.DoesNotContain(token, json.GetRawText());
        var me = await app.Client.GetAsync("/api/auth/me");
        Assert.Equal(200, (int)me.StatusCode);
        Assert.Equal(json.GetRawText(), (await me.Content.ReadFromJsonAsync<JsonElement>()).GetRawText());
        Assert.False(me.Headers.Contains("Set-Cookie"));
    }

    [Theory]
    [InlineData("missing@example.test", Password, false)]
    [InlineData("jan.novak@example.test", "nespravne-dlouhe-heslo", false)]
    [InlineData("jan.novak@example.test", Password, true)]
    public async Task InvalidCredentials_UseIdentical401WithoutCreatingSession(string email, string password, bool inactive)
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        if (inactive) await app.Database.ExecuteAsync("UPDATE app_user SET is_active = false");
        var body = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/login", Login(email, password)), 401, "INVALID_CREDENTIALS");
        Assert.Equal("E-mail nebo heslo nejsou správné.", body.GetProperty("detail").GetString());
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Me_RejectsExactExpiryWithoutSlidingExtension(bool persistent)
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        var login = await LoginAsync(app.Client, persistent);
        var token = Token(login); using var replay = Replay(app, token);
        var expiry = await app.Database.ScalarAsync<DateTime>("SELECT expires_at FROM user_session");
        // PostgreSQL zaokrouhluje na mikrosekundy, testuje se přesný uložený okamžik.
        app.Clock.Advance(new DateTimeOffset(expiry) - app.Clock.GetUtcNow() - TimeSpan.FromSeconds(1));
        Assert.Equal(200, (int)(await replay.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(expiry, await app.Database.ScalarAsync<DateTime>("SELECT expires_at FROM user_session"));
        app.Clock.Advance(TimeSpan.FromSeconds(1));
        await ProblemAsync(await replay.GetAsync("/api/auth/me"), 401, "UNAUTHENTICATED");
    }

    [Fact]
    public async Task NewLogin_RotatesCurrentCookieButKeepsOtherDeviceActive()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        var first = Token(await LoginAsync(app.Client));
        using var device = app.CreateClient();
        var other = Token(await LoginAsync(device));
        var latest = Token(await LoginAsync(app.Client));
        Assert.NotEqual(first, latest); Assert.NotEqual(other, latest);
        using var stale = Replay(app, first); using var valid = Replay(app, other);
        await ProblemAsync(await stale.GetAsync("/api/auth/me"), 401, "UNAUTHENTICATED");
        Assert.Equal(200, (int)(await valid.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(200, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
    }

    [Fact]
    public async Task Logout_RevokesBefore204_AndRejectsReplay_AndIsIdempotent()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        var token = Token(await LoginAsync(app.Client));
        await app.SetCsrfAsync();
        var response = await app.Client.PostAsync("/api/auth/logout", null);
        Assert.Equal(204, (int)response.StatusCode); Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.Contains(CookieName + "=;", response.Headers.GetValues("Set-Cookie").Single());
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
        using var replay = Replay(app, token);
        await ProblemAsync(await replay.GetAsync("/api/auth/me"), 401, "UNAUTHENTICATED");
        await app.SetCsrfAsync();
        Assert.Equal(204, (int)(await app.Client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task LoginAndLogout_RequireCsrfBoundToCurrentIdentity()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/login", Login()), 403, "CSRF_INVALID");
        await SeedAsync(app); Assert.Equal(200, (int)(await app.Client.PostAsJsonAsync("/api/auth/login", Login())).StatusCode);
        // Hlavička ještě obsahuje token anonymní identity.
        await ProblemAsync(await app.Client.PostAsync("/api/auth/logout", null), 403, "CSRF_INVALID");
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
        await app.SetCsrfAsync(); Assert.Equal(204, (int)(await app.Client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("forged")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("------------------------------------------!")]
    public async Task MissingOrForgedCookie_Returns401WithoutHtmlRedirect(string token)
    {
        await using var app = await RegistrationApp.CreateAsync(); using var client = Replay(app, token);
        await ProblemAsync(await client.GetAsync("/api/auth/me"), 401, "UNAUTHENTICATED");
        await ProblemAsync(await client.GetAsync("/api/tasks"), 401, "UNAUTHENTICATED");
    }

    [Fact]
    public async Task Logout_RequiresHeaderAndDoesNotAcceptFormTokenAsSubstitute()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app); await LoginAsync(app.Client);
        await app.SetCsrfAsync();
        var token = app.Client.DefaultRequestHeaders.GetValues("X-CSRF-TOKEN").Single();
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        await ProblemAsync(await app.Client.PostAsync("/api/auth/logout", form), 403, "CSRF_INVALID");
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
    }

    [Fact]
    public async Task DeactivatedOwner_LosesExistingSession()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app); await LoginAsync(app.Client);
        await app.Database.ExecuteAsync("UPDATE app_user SET is_active = false");
        await ProblemAsync(await app.Client.GetAsync("/api/auth/me"), 401, "UNAUTHENTICATED");
    }

    [Fact]
    public async Task LegacyHash_IsRehashedDuringSuccessfulLogin()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        var hash = new PasswordHasher<object>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 })).HashPassword(new object(), Password);
        await using (var command = app.Database.DataSource.CreateCommand("UPDATE app_user SET password_hash = $1"))
        { command.Parameters.AddWithValue(hash); await command.ExecuteNonQueryAsync(); }
        Assert.Equal(200, (int)(await LoginAsync(app.Client)).StatusCode);
        var stored = await new PostgresUserAccountStore(app.Database.DataSource).FindByEmailAsync("jan.novak@example.test");
        Assert.NotEqual(hash, stored!.PasswordHash);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(Convert.FromBase64String(stored.PasswordHash).AsSpan(5, 4)) >= 220_000);
        Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<object>().VerifyHashedPassword(new object(), stored.PasswordHash, Password));
    }

    [Fact]
    public async Task DatabaseFailure_NeverClaimsSuccessfulLoginOrLogoutOrUnauthenticated()
    {
        await using var app = await RegistrationApp.CreateAsync(); await SeedAsync(app);
        app.FailSessionCreates = true;
        var failed = await LoginAsync(app.Client); await ProblemAsync(failed, 503, "SERVICE_UNAVAILABLE");
        Assert.False(failed.Headers.Contains("Set-Cookie"));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
        app.FailSessionCreates = false; await LoginAsync(app.Client);
        app.FailSessionReads = true;
        await ProblemAsync(await app.Client.GetAsync("/api/auth/me"), 503, "SERVICE_UNAVAILABLE");
        app.FailSessionReads = false; await app.SetCsrfAsync(); app.FailSessionRevocations = true;
        var logout = await app.Client.PostAsync("/api/auth/logout", null); await ProblemAsync(logout, 503, "SERVICE_UNAVAILABLE");
        Assert.False(logout.Headers.Contains("Set-Cookie"));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
        app.FailSessionRevocations = false;
        Assert.Equal(204, (int)(await app.Client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task LoginRateLimit_ReturnsRetryAfterWithoutDisablingAccount()
    {
        await using var app = await RegistrationApp.CreateAsync(loginPermitLimit: 1); await SeedAsync(app);
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/login", Login(password: "nespravne-dlouhe-heslo")), 401, "INVALID_CREDENTIALS");
        var response = await app.Client.PostAsJsonAsync("/api/auth/login", Login());
        await ProblemAsync(response, 429, "RATE_LIMITED"); Assert.True(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds > 0);
        Assert.True(await app.Database.ScalarAsync<bool>("SELECT is_active FROM app_user"));
    }

    [Theory]
    [InlineData("{\"rememberMe\":\"true\"}")]
    [InlineData("{\"accountType\":\"ADMIN\"}")]
    [InlineData("{\"password\":[]}")]
    [InlineData("null")]
    public async Task InvalidLoginJson_ReturnsUnified400(string json)
    {
        await using var app = await RegistrationApp.CreateAsync(); await app.SetCsrfAsync();
        await ProblemAsync(await app.Client.PostAsync("/api/auth/login", new StringContent(json, Encoding.UTF8, "application/json")), 400, "VALIDATION_FAILED");
    }

    [Fact]
    public async Task LoginBodyLimitAndContentType_AreEnforced()
    {
        await using var app = await RegistrationApp.CreateAsync(); await app.SetCsrfAsync();
        await ProblemAsync(await app.Client.PostAsync("/api/auth/login", new StringContent("{}")), 415, "UNSUPPORTED_MEDIA_TYPE");
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/login", Login(password: new string('x', 20_000))), 413, "PAYLOAD_TOO_LARGE");
    }
}
