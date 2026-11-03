using System.Net.Http.Json;
using System.Text.Json;

namespace StudentFlow.Api.IntegrationTests;

public class HandoffExampleTests
{
    [Fact]
    public async Task ExampleEndpoint_RejectsAnonymousRequestWithJson401()
    {
        await using var app = await RegistrationApp.CreateAsync(includeHandoffExample: true);
        var response = await app.Client.GetAsync("/api/account/summary");
        Assert.Equal(401, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("UNAUTHENTICATED", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("STUDENT", 403)]
    [InlineData("TEACHER", 200)]
    public async Task ExampleEndpoint_UsesSessionOwnerAndChecksAccountType(string accountType, int teachingStatus)
    {
        await using var app = await RegistrationApp.CreateAsync(includeHandoffExample: true);
        await app.SetCsrfAsync();
        Assert.Equal(201, (int)(await app.Client.PostAsJsonAsync("/api/auth/register", RegistrationApp.Valid(type: accountType))).StatusCode);
        Assert.Equal(200, (int)(await app.Client.PostAsJsonAsync("/api/auth/login", new { email = "jan.novak@example.test", password = "moje-dlouhe-heslo-2026" })).StatusCode);
        var response = await app.Client.GetAsync("/api/account/summary?userId=999999");
        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        var user = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user");
        Assert.Equal("jan.novak@example.test", user.GetProperty("email").GetString());
        Assert.Equal(accountType, user.GetProperty("accountType").GetString());
        Assert.NotEqual("999999", user.GetProperty("id").GetString());
        Assert.Equal(4, user.EnumerateObject().Count());
        var restricted = await app.Client.GetAsync("/api/account/summary/teaching");
        Assert.Equal(teachingStatus, (int)restricted.StatusCode);
        if (teachingStatus == 403)
            Assert.Equal("FORBIDDEN", (await restricted.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(200, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);
        await app.SetCsrfAsync();
        Assert.Equal(204, (int)(await app.Client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(401, (int)(await app.Client.GetAsync("/api/account/summary")).StatusCode);
    }
}
