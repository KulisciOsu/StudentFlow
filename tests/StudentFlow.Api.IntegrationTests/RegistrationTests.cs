using System.Buffers.Binary;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using StudentFlow.Data.Authentication;
using StudentFlow.Core.Authentication;

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
        const string password = "TestHeslo1!";
        var response = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(" JAN.NOVAK@EXAMPLE.TEST ", type, password));
        Assert.Equal(201, (int)response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var user = json.GetProperty("user");
        Assert.Equal("jan.novak@example.test", user.GetProperty("email").GetString());
        Assert.Equal("Jan Novák", user.GetProperty("displayName").GetString());
        Assert.Equal("Jan", await app.Database.ScalarAsync<string>("SELECT first_name FROM app_user"));
        Assert.Equal("Novák", await app.Database.ScalarAsync<string>("SELECT last_name FROM app_user"));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE display_name IS NULL"));
        Assert.Equal(type, user.GetProperty("accountType").GetString());
        Assert.Equal(JsonValueKind.String, user.GetProperty("id").ValueKind);
        Assert.Equal(4, user.EnumerateObject().Count());
        Assert.DoesNotContain(password, json.GetRawText());
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var cookies) && cookies.Any(c => c.StartsWith("__Host-StudentFlow.Session=")));
        var stored = await new PostgresUserAccountStore(app.Database.DataSource).FindByEmailAsync("jan.novak@example.test");
        Assert.NotNull(stored);
        var hasher = new PasswordHasher<object>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(new object(), stored.PasswordHash, password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(new object(), stored.PasswordHash, password.ToLowerInvariant()));
        Assert.Equal(RegistrationPrivacyPolicy.Version, await app.Database.ScalarAsync<string>("SELECT privacy_policy_version FROM app_user"));
        var acceptedAt = await app.Database.ScalarAsync<DateTime>("SELECT privacy_accepted_at FROM app_user");
        Assert.Equal(app.Clock.GetUtcNow().ToUnixTimeMilliseconds(), new DateTimeOffset(acceptedAt).ToUnixTimeMilliseconds());
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
        var conflict = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(" JAN.NOVAK@EXAMPLE.TEST ", "TEACHER")), 409, "REGISTRATION_CONFLICT");
        Assert.DoesNotContain("už existuje", conflict.GetRawText());
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    [Theory]
    [InlineData("firstName", " ")]
    [InlineData("lastName", " ")]
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
    [InlineData("firstName", "")]
    [InlineData("lastName", "")]
    [InlineData("firstName", "Jan Novák")]
    [InlineData("lastName", "Novák Svoboda")]
    [InlineData("firstName", " Jan")]
    [InlineData("lastName", "Novák ")]
    [InlineData("firstName", "Jan\tPavel")]
    [InlineData("lastName", "Novák\nSvoboda")]
    [InlineData("firstName", "Jan\u00A0Pavel")]
    [InlineData("lastName", "Novák\u0085Svoboda")]
    [InlineData("firstName", "Jan\uFEFFPavel")]
    public async Task NameParts_RejectWhitespaceWithoutSilentlyTrimming(string field, string value)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload[field] = value;
        var problem = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("firstName", 1, 201)]
    [InlineData("firstName", 50, 201)]
    [InlineData("firstName", 51, 400)]
    [InlineData("lastName", 1, 201)]
    [InlineData("lastName", 50, 201)]
    [InlineData("lastName", 51, 400)]
    public async Task NameParts_UseUnicodeCodePointLength(string field, int length, int status)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload[field] = string.Concat(Enumerable.Repeat("𐐀", length));
        Assert.Equal(status, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
    }

    [Theory]
    [InlineData("firstName", "Jan1")]
    [InlineData("lastName", "Novák2")]
    [InlineData("firstName", "Jan١")]
    [InlineData("lastName", "Novák１")]
    [InlineData("firstName", "JanⅣ")]
    [InlineData("lastName", "Novák-Svoboda")]
    [InlineData("firstName", "O’Neill")]
    [InlineData("lastName", "O'Neill")]
    [InlineData("firstName", "Jan@")]
    [InlineData("lastName", "Novák_")]
    [InlineData("firstName", "Jan🦉")]
    [InlineData("lastName", "\u0301Novák")]
    [InlineData("firstName", "A\u20DD")]
    public async Task NameParts_RejectDigitsAndSpecialCharacters(string field, string value)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload[field] = value;
        var problem = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task NameParts_AcceptDiacriticsWithoutRewritingTheirUnicodeRepresentation()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload["firstName"] = "Žofie";
        payload["lastName"] = "Nova\u0301k";
        Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", payload)).StatusCode);
        Assert.Equal("Žofie", await app.Database.ScalarAsync<string>("SELECT first_name FROM app_user"));
        Assert.Equal("Nova\u0301k", await app.Database.ScalarAsync<string>("SELECT last_name FROM app_user"));
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    public async Task MissingNamePart_IsRejected(string field)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload.Remove(field);
        var problem = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.True(problem.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task LegacyDisplayName_CannotReplaceBothNameParts()
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        payload.Remove("firstName");
        payload.Remove("lastName");
        payload["displayName"] = "Jan Novák";
        await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData(7, 400)]
    [InlineData(8, 201)]
    [InlineData(15, 201)]
    [InlineData(16, 400)]
    public async Task PasswordLength_UsesUnicodeCodePoints(int length, int status)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var password = "A1!" + string.Concat(Enumerable.Repeat("🦉", length - 3));
        var response = await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(password: password));
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(status == 201 ? 1L : 0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("testheslo1!")]
    [InlineData("TestHeslo!!")]
    [InlineData("TestHeslo12")]
    [InlineData(" TestHeslo1!")]
    [InlineData("TestHeslo1! ")]
    [InlineData("Test\tHeslo1!")]
    [InlineData("Test\nHeslo1!")]
    [InlineData("Test\u00a0Heslo1!")]
    [InlineData("Test\u0085Heslo1!")]
    [InlineData("Test\ufeffHeslo1!")]
    public async Task PasswordRules_RejectIncompleteOrWhitespacePasswordsWithoutWriting(string password)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var body = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(password: password)), 400, "VALIDATION_FAILED");
        Assert.True(body.GetProperty("errors").TryGetProperty("password", out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("false")]
    [InlineData("missing-version")]
    [InlineData("old-version")]
    public async Task PrivacyAcceptance_IsRequiredAndBoundToCurrentVersion(string invalid)
    {
        await using var app = await RegistrationApp.CreateAsync();
        await app.SetCsrfAsync();
        var payload = RegistrationApp.Valid();
        if (invalid == "missing") payload.Remove("privacyAccepted");
        if (invalid == "false") payload["privacyAccepted"] = false;
        if (invalid == "missing-version") payload.Remove("privacyPolicyVersion");
        if (invalid == "old-version") payload["privacyPolicyVersion"] = "old-policy";
        var body = await ProblemAsync(await app.Client.PostAsJsonAsync("/api/auth/register", payload), 400, "VALIDATION_FAILED");
        Assert.True(body.GetProperty("errors").TryGetProperty("privacyAccepted", out _));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
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
    [InlineData("{\"firstName\":\"A\",\"lastName\":\"Test\",\"email\":\"a@example.test\",\"password\":123,\"accountType\":\"STUDENT\"}")]
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
        Assert.DoesNotContain("TestHeslo1!", problem.GetRawText());
        Assert.Contains(app.Logs, message => message.Contains("Auth request failed.") && message.Contains("NpgsqlException"));
        foreach (var message in app.Logs)
        {
            Assert.DoesNotContain("sensitive-", message);
            Assert.DoesNotContain("TestHeslo1!", message);
        }
    }
}
