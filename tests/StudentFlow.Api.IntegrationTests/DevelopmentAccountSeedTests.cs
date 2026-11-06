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
    private const string Password = "StudentFlow-Test-2026!";

    [Theory]
    [InlineData("tester@studentflow.test", "Testovací člen", AccountType.Member, "STUDENT")]
    [InlineData("teacher@studentflow.test", "Testovací vyučující", AccountType.Teacher, "TEACHER")]
    public async Task DevelopmentStartup_CreatesUsableAccountsAndRepeatedSeedPreservesThem(
        string email, string displayName, AccountType accountType, string typeCode)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development");
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var credentials = (await accounts.FindByEmailAsync(email))!;
        Assert.NotNull(credentials);
        Assert.Equal(displayName, credentials.User.DisplayName);
        Assert.Equal(accountType, credentials.User.AccountType);
        Assert.True(credentials.User.IsActive);
        Assert.NotEqual(Password, credentials.PasswordHash);
        Assert.Equal(new[] { typeCode }, await accounts.GetRoleCodesAsync(credentials.User.Id));
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(401, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await app.SetCsrfAsync();
        var response = await app.Client.PostAsJsonAsync("/api/auth/login", new { email, password = Password, rememberMe = false });
        Assert.Equal(200, (int)response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal(typeCode, user.GetProperty("accountType").GetString());
        Assert.Equal(200, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await app.SetCsrfAsync();
        Assert.Equal(204, (int)(await app.Client.PostAsJsonAsync("/api/auth/logout", new { })).StatusCode);
        Assert.Equal(401, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await DevelopmentAccountSeeder.SeedAsync(app.Services);
        var repeated = (await accounts.FindByEmailAsync(email))!;
        Assert.Equal(credentials.User, repeated.User);
        Assert.Equal(credentials.PasswordHash, repeated.PasswordHash);
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        Assert.DoesNotContain(app.Logs, message => message.Contains(Password, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("tester@studentflow.test", AccountType.Teacher, "TEACHER", "teacher@studentflow.test", AccountType.Teacher)]
    [InlineData("teacher@studentflow.test", AccountType.Member, "STUDENT", "tester@studentflow.test", AccountType.Member)]
    public async Task Seed_PreservesExistingAccountAndCreatesMissingAccount(
        string email, AccountType existingType, string existingTypeCode, string missingEmail, AccountType missingType)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false);
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var created = await accounts.CreateAsync(new("Vlastní účet", $" {email.ToUpperInvariant()} ", "existing-hash", existingType));
        await app.Database.ExecuteAsync("UPDATE app_user SET is_active = false");
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        await DevelopmentAccountSeeder.SeedAsync(app.Services);

        var credentials = (await accounts.FindByEmailAsync(email))!;
        Assert.Equal(created.User!.Id, credentials.User.Id);
        Assert.Equal("Vlastní účet", credentials.User.DisplayName);
        Assert.Equal("existing-hash", credentials.PasswordHash);
        Assert.Equal(existingType, credentials.User.AccountType);
        Assert.False(credentials.User.IsActive);
        Assert.Equal(new[] { existingTypeCode }, await accounts.GetRoleCodesAsync(credentials.User.Id));
        var added = await accounts.FindByEmailAsync(missingEmail);
        Assert.NotNull(added);
        Assert.Equal(missingType, added.User.AccountType);
        Assert.True(added.User.IsActive);
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
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
    public async Task ConcurrentSeeds_CreateEachAccountAndTypeOnlyOnce()
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false);
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        await Task.WhenAll(DevelopmentAccountSeeder.SeedAsync(app.Services), DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var member = await accounts.FindByEmailAsync("tester@studentflow.test");
        var teacher = await accounts.FindByEmailAsync("teacher@studentflow.test");
        Assert.NotNull(member);
        Assert.NotNull(teacher);
        Assert.Equal(AccountType.Member, member.User.AccountType);
        Assert.Equal(AccountType.Teacher, teacher.User.AccountType);
        Assert.Equal(new[] { "STUDENT" }, await accounts.GetRoleCodesAsync(member.User.Id));
        Assert.Equal(new[] { "TEACHER" }, await accounts.GetRoleCodesAsync(teacher.User.Id));
    }
}
