using System.Buffers.Binary;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Api.IntegrationTests;

public class RegistrationTests
{
    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var value = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, value.GetProperty("status").GetInt32());
        Assert.Equal(code, value.GetProperty("code").GetString());
        Assert.Equal("about:blank", value.GetProperty("type").GetString());
        Assert.False(string.IsNullOrEmpty(value.GetProperty("traceId").GetString()));
        return value;
    }

    [Fact]
    public async Task Csrf_SetsSecureHttpOnlyHostCookie_AndDoesNotCache()
    {
        await using var app = await RegistrationApp.CreateAsync();
        var response = await app.SetCsrfAsync();
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("__Host-StudentFlow.Csrf=", cookie);
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookie.ToLowerInvariant());
        Assert.Contains("path=/", cookie.ToLowerInvariant());
        Assert.DoesNotContain("domain=", cookie.ToLowerInvariant());
    }

    [Theory]
    [InlineData("STUDENT")]
    [InlineData("TEACHER")]
    public async Task Register_PersistsNormalizedAccountAndSaltedHash_WithoutSessionOrSecrets(string type)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        const string password = "  heslova-fraze-2026  ";
        var response = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(" JAN.NOVAK@EXAMPLE.TEST ", type, password));
        Assert.Equal(201, (int)response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var user = json.GetProperty("user");
        Assert.Equal("jan.novak@example.test", user.GetProperty("email").GetString());
        Assert.Equal("Jan Novák", user.GetProperty("displayName").GetString());
        Assert.Equal(type, user.GetProperty("accountType").GetString());
        Assert.Equal(JsonValueKind.String, user.GetProperty("id").ValueKind);
        Assert.Equal(4, user.EnumerateObject().Count());
        Assert.DoesNotContain(password, json.GetRawText());
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("__Host-StudentFlow.Session=")));
        var stored = await new PostgresUserAccountStore(app.Database.DataSource).FindByEmailAsync("jan.novak@example.test");
        Assert.NotNull(stored);
        var hasher = new PasswordHasher<object>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(new object(), stored.PasswordHash, password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(new object(), stored.PasswordHash, password.Trim()));
        var bytes = Convert.FromBase64String(stored.PasswordHash);
        Assert.Equal(1, bytes[0]);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(5, 4)) >= 220_000);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
        var other = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid("other@example.test", type, password));
        Assert.Equal(201, (int)other.StatusCode);
        var otherStored = await new PostgresUserAccountStore(app.Database.DataSource).FindByEmailAsync("other@example.test");
        Assert.NotEqual(stored.PasswordHash, otherStored!.PasswordHash);
    }

    [Fact]
    public async Task Duplicate_Returns409AndKeepsOneAccountAndRole()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid())).StatusCode);
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(" JAN.NOVAK@EXAMPLE.TEST ", "TEACHER")), 409, "EMAIL_ALREADY_REGISTERED");
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    [Theory]
    [InlineData("displayName", " ")]
    [InlineData("email", "invalid-address")]
    [InlineData("accountType", "ADMIN")]
    [InlineData("accountType", "TEAM_LEADER")]
    [InlineData("accountType", "STUDENT,TEACHER")]
    [InlineData("accountType", "")]
    [InlineData("password", "too-short")]
    public async Task InvalidFields_Return400WithSafeFieldErrors(string field, string value)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid(); payload[field] = value;
        var problem = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData(14, 400)]
    [InlineData(15, 201)]
    [InlineData(128, 201)]
    [InlineData(129, 400)]
    public async Task PasswordLength_UsesUnicodeCodePoints(int length, int status)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var response = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(password: string.Concat(Enumerable.Repeat("🦉", length))));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(status == 201 ? 1L : 0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task MissingOrForgedCsrf_IsRejectedBeforeWriting()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid()), 403, "CSRF_INVALID");
        await app.SetCsrfAsync();
        app.Client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        app.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "forged-token");
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid()), 403, "CSRF_INVALID");
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"displayName\":\"A\",\"email\":\"a@example.test\",\"password\":123,\"accountType\":\"STUDENT\"}")]
    [InlineData("{\"accountTypes\":[\"STUDENT\",\"TEACHER\"]}")]
    [InlineData("{\"accountType\":[\"STUDENT\",\"TEACHER\"]}")]
    [InlineData("null")]
    public async Task MalformedOrUnknownJson_UsesUnified400(string body)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        await ProblemAsync(await app.Client.PostAsync("/api/auth/register", new StringContent(body, Encoding.UTF8, "application/json")), 400, "VALIDATION_FAILED");
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task UnsupportedMediaAndOversizedBody_UseUnifiedErrors()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        await ProblemAsync(await app.Client.PostAsync("/api/auth/register", new StringContent("{}", Encoding.UTF8, "text/plain")), 415, "UNSUPPORTED_MEDIA_TYPE");
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(password: new string('x', 20_000))), 413, "PAYLOAD_TOO_LARGE");
    }

    [Fact]
    public async Task RateLimit_Returns429WithRetryAfter()
    {
        await using var app = await RegistrationApp.CreateAsync(permitLimit: 1);
        await app.SetCsrfAsync();
        Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid())).StatusCode);
        var response = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid("other@example.test"));
        await ProblemAsync(response, 429, "RATE_LIMITED");
        Assert.True(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds > 0);
    }

    [Fact]
    public async Task DatabaseUnavailable_Returns503WithoutExceptionDetailsOrPassword()
    {
        await using var app = await RegistrationApp.CreateAsync(failWrites: true);
        await app.SetCsrfAsync();
        var problem = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid()), 503, "SERVICE_UNAVAILABLE");
        Assert.DoesNotContain("sensitive-", problem.GetRawText());
        Assert.DoesNotContain("moje-dlouhe-heslo", problem.GetRawText());
        Assert.Contains(app.Logs, message => message.Contains("Auth request failed.") && message.Contains("NpgsqlException"));
        foreach (var message in app.Logs)
        {
            Assert.DoesNotContain("sensitive-", message);
            Assert.DoesNotContain("moje-dlouhe-heslo-2026", message);
        }
    }
}
