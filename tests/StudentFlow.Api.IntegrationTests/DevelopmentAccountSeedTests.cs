using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentFlow.Api.Development;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Api.IntegrationTests;

public class DevelopmentAccountSeedTests
{
    private const string Email = "tester@studentflow.test";
    private const string Password = "StudentFlow-Test-2026!";

    [Fact]
    public async Task DevelopmentStartup_CreatesUsableMemberAndRepeatedSeedPreservesIt()
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development");
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var credentials = (await accounts.FindByEmailAsync(Email))!;
        Assert.NotNull(credentials);
        Assert.Equal("Testovací člen", credentials.User.DisplayName);
        Assert.Equal(AccountType.Member, credentials.User.AccountType);
        Assert.True(credentials.User.IsActive);
        Assert.NotEqual(Password, credentials.PasswordHash);
        Assert.Equal(new[] { "STUDENT" }, await accounts.GetRoleCodesAsync(credentials.User.Id));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(401, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await app.SetCsrfAsync();
        var response = await app.Client.PostAsJsonAsync("/api/auth/login", new { email = Email, password = Password, rememberMe = false });
        Assert.Equal(200, (int)response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user");
        Assert.Equal(Email, user.GetProperty("email").GetString());
        Assert.Equal("STUDENT", user.GetProperty("accountType").GetString());
        Assert.Equal(200, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await DevelopmentAccountSeeder.SeedAsync(app.Services);
        var repeated = (await accounts.FindByEmailAsync(Email))!;
        Assert.Equal(credentials.User, repeated.User);
        Assert.Equal(credentials.PasswordHash, repeated.PasswordHash);
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        Assert.DoesNotContain(app.Logs, message => message.Contains(Password, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Seed_PreservesExistingAccountIncludingPasswordTypeAndActivity()
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false);
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var created = await accounts.CreateAsync(new("Vlastní účet", " TESTER@STUDENTFLOW.TEST ", "existing-hash", AccountType.Teacher));
        await app.Database.ExecuteAsync("UPDATE app_user SET is_active = false");
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        await DevelopmentAccountSeeder.SeedAsync(app.Services);

        var credentials = (await accounts.FindByEmailAsync(Email))!;
        Assert.Equal(created.User!.Id, credentials.User.Id);
        Assert.Equal("Vlastní účet", credentials.User.DisplayName);
        Assert.Equal("existing-hash", credentials.PasswordHash);
        Assert.Equal(AccountType.Teacher, credentials.User.AccountType);
        Assert.False(credentials.User.IsActive);
        Assert.Equal(new[] { "TEACHER" }, await accounts.GetRoleCodesAsync(credentials.User.Id));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Testing", true)]
    [InlineData("Development", false)]
    public async Task Startup_DoesNotSeedOutsideEnabledDevelopment(string environment, bool enabled)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: environment, seedEnabled: enabled);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task ConcurrentSeeds_CreateOnlyOneAccountAndType()
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false);
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        await Task.WhenAll(DevelopmentAccountSeeder.SeedAsync(app.Services), DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(1L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        var account = await new PostgresUserAccountStore(app.Database.DataSource).FindByEmailAsync(Email);
        Assert.NotNull(account);
        Assert.Equal(AccountType.Member, account.User.AccountType);
    }
}
