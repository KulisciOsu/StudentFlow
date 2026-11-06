using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentFlow.Api.Development;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>
/// Ověřuje vývojový seed přes skutečnou HTTP pipeline a izolované PostgreSQL databáze.
/// </summary>
/// <remarks>
/// Pokrývá oba typy účtu, konfiguraci hesla, zachování existujících dat a opakované i souběžné spuštění.
/// </remarks>
public class DevelopmentAccountSeedTests
{
    private const string MemberEmailKey = "DevelopmentSeed:MemberEmail";
    private const string TeacherEmailKey = "DevelopmentSeed:TeacherEmail";
    private const string PasswordKey = "DevelopmentSeed:Password";

    /// <summary>Ověří přihlášení, odhlášení a zachování identity i hashe po opakování vývojového seedu.</summary>
    /// <param name="useTeacherEmail">Zda ověřit e-mail vyučujícího místo člena z vlastní testovací konfigurace.</param>
    /// <param name="displayName">Očekávané zobrazované jméno účtu.</param>
    /// <param name="accountType">Očekávaný doménový typ účtu.</param>
    /// <param name="typeCode">Očekávaný kód typu v databázových rolích a odpovědi přihlášení.</param>
    /// <returns>Úlohu dokončenou po ověření účtu a opakovaného seedu.</returns>
    [Theory]
    [InlineData(false, "Testovací člen", AccountType.Member, "STUDENT")]
    [InlineData(true, "Testovací vyučující", AccountType.Teacher, "TEACHER")]
    public async Task DevelopmentStartup_CreatesUsableAccountsAndRepeatedSeedPreservesThem(
        bool useTeacherEmail, string displayName, AccountType accountType, string typeCode)
    {
        var settings = CreateSeedConfiguration();
        var email = settings[useTeacherEmail ? TeacherEmailKey : MemberEmailKey]!;
        var password = settings[PasswordKey]!;
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: true, seedConfiguration: settings);
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var credentials = (await accounts.FindByEmailAsync(email))!;
        Assert.NotNull(credentials);
        Assert.Equal(displayName, credentials.User.DisplayName);
        Assert.Equal(accountType, credentials.User.AccountType);
        Assert.True(credentials.User.IsActive);
        Assert.NotEqual(password, credentials.PasswordHash);
        Assert.Equal(new[] { typeCode }, await accounts.GetRoleCodesAsync(credentials.User.Id));
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(401, (int)(await app.Client.GetAsync("/api/auth/me")).StatusCode);

        await app.SetCsrfAsync();
        var response = await app.Client.PostAsJsonAsync("/api/auth/login", new { email, password, rememberMe = false });
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
        Assert.DoesNotContain(app.Logs, message => message.Contains(password, StringComparison.Ordinal));
    }

    /// <summary>Ověří, že kolidující účet zůstane nezměněný a druhý chybějící účet se přesto vytvoří.</summary>
    /// <param name="existingTeacherEmail">Zda obsadit e-mail vyučujícího místo člena z testovací konfigurace.</param>
    /// <param name="existingType">Původní typ kolidujícího účtu, který seed nesmí změnit.</param>
    /// <param name="existingTypeCode">Očekávaný zachovaný kód databázové role existujícího účtu.</param>
    /// <param name="missingType">Očekávaný typ doplněného účtu.</param>
    /// <returns>Úlohu dokončenou po ověření zachovaných i nově vytvořených dat.</returns>
    [Theory]
    [InlineData(false, AccountType.Teacher, "TEACHER", AccountType.Teacher)]
    [InlineData(true, AccountType.Member, "STUDENT", AccountType.Member)]
    public async Task Seed_PreservesExistingAccountAndCreatesMissingAccount(
        bool existingTeacherEmail, AccountType existingType, string existingTypeCode, AccountType missingType)
    {
        var settings = CreateSeedConfiguration();
        var email = settings[existingTeacherEmail ? TeacherEmailKey : MemberEmailKey]!;
        var missingEmail = settings[existingTeacherEmail ? MemberEmailKey : TeacherEmailKey]!;
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: settings);
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

    /// <summary>Ověří, že mimo povolené vývojové prostředí nevzniknou žádné testovací účty.</summary>
    /// <param name="environment">Prostředí hostitele API, například Production nebo Development.</param>
    /// <param name="enabled">Hodnota přepínače DevelopmentSeed:Enabled pro daný scénář.</param>
    /// <returns>Úlohu dokončenou po kontrole prázdné tabulky účtů.</returns>
    [Theory]
    [InlineData("Production", true)]
    [InlineData("Testing", true)]
    [InlineData("Development", false)]
    public async Task Startup_DoesNotSeedOutsideEnabledDevelopment(string environment, bool enabled)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: environment, seedEnabled: enabled);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    /// <summary>Ověří, že souběžné spuštění založí každý účet právě jednou s jeho jedinou správnou rolí.</summary>
    /// <returns>Úlohu dokončenou po obou spuštěních a kontrole účtů i rolí.</returns>
    [Fact]
    public async Task ConcurrentSeeds_CreateEachAccountAndTypeOnlyOnce()
    {
        var settings = CreateSeedConfiguration();
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: settings);
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        await Task.WhenAll(DevelopmentAccountSeeder.SeedAsync(app.Services), DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(2L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var member = await accounts.FindByEmailAsync(settings[MemberEmailKey]!);
        var teacher = await accounts.FindByEmailAsync(settings[TeacherEmailKey]!);
        Assert.NotNull(member);
        Assert.NotNull(teacher);
        Assert.Equal(AccountType.Member, member.User.AccountType);
        Assert.Equal(AccountType.Teacher, teacher.User.AccountType);
        Assert.Equal(new[] { "STUDENT" }, await accounts.GetRoleCodesAsync(member.User.Id));
        Assert.Equal(new[] { "TEACHER" }, await accounts.GetRoleCodesAsync(teacher.User.Id));
    }

    /// <summary>Ověří použití vlastního hesla včetně mezer a zachování hashe po pozdější změně konfigurace.</summary>
    /// <param name="useTeacherEmail">Zda ověřit účet vyučujícího místo člena z testovací konfigurace.</param>
    /// <param name="typeCode">Očekávaný kód typu účtu v odpovědi přihlášení.</param>
    /// <returns>Úlohu dokončenou po ověření konfigurovaného hesla a opakovaného seedu.</returns>
    [Theory]
    [InlineData(false, "STUDENT")]
    [InlineData(true, "TEACHER")]
    public async Task Seed_UsesConfiguredPasswordAndPreservesItWhenConfigurationChanges(bool useTeacherEmail, string typeCode)
    {
        var settings = CreateSeedConfiguration();
        var email = settings[useTeacherEmail ? TeacherEmailKey : MemberEmailKey]!;
        var rejectedPassword = settings[PasswordKey]!;
        var customPassword = $"  {Guid.NewGuid():N}  ";
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: settings);
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        configuration["DevelopmentSeed:Enabled"] = "true";
        configuration[PasswordKey] = customPassword;

        await DevelopmentAccountSeeder.SeedAsync(app.Services);
        var accounts = new PostgresUserAccountStore(app.Database.DataSource);
        var created = (await accounts.FindByEmailAsync(email))!;
        var member = (await accounts.FindByEmailAsync(settings[MemberEmailKey]!))!;
        var teacher = (await accounts.FindByEmailAsync(settings[TeacherEmailKey]!))!;
        Assert.NotEqual(customPassword, created.PasswordHash);
        Assert.NotEqual(member.PasswordHash, teacher.PasswordHash);
        configuration[PasswordKey] = Guid.NewGuid().ToString("N");
        await DevelopmentAccountSeeder.SeedAsync(app.Services);
        Assert.Equal(created.PasswordHash, (await accounts.FindByEmailAsync(email))!.PasswordHash);

        await app.SetCsrfAsync();
        var rejectedLogin = await app.Client.PostAsJsonAsync("/api/auth/login", new { email, password = rejectedPassword, rememberMe = false });
        Assert.Equal(401, (int)rejectedLogin.StatusCode);
        var login = await app.Client.PostAsJsonAsync("/api/auth/login", new { email, password = customPassword, rememberMe = false });
        Assert.Equal(200, (int)login.StatusCode);
        var user = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal(typeCode, user.GetProperty("accountType").GetString());
        Assert.DoesNotContain(app.Logs, message => message.Contains(customPassword, StringComparison.Ordinal));
    }

    /// <summary>Ověří odmítnutí chybějícího hesla nebo hesla s neplatnou délkou před jakýmkoli zápisem účtů.</summary>
    /// <param name="passwordLength">Počet znaků zkušebního hesla; hodnota -1 představuje chybějící konfiguraci.</param>
    /// <returns>Úlohu dokončenou po ověření chyby konfigurace a absence zápisů.</returns>
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(129)]
    public async Task Seed_RejectsInvalidPasswordBeforeCreatingAccounts(int passwordLength)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: CreateSeedConfiguration());
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        configuration["DevelopmentSeed:Enabled"] = "true";
        var password = passwordLength < 0 ? null : new string('x', passwordLength);
        configuration[PasswordKey] = password;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Contains(PasswordKey, exception.Message);
        if (!string.IsNullOrEmpty(password)) Assert.DoesNotContain(password, exception.Message);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    /// <summary>Ověří odmítnutí chybějícího nebo neplatného e-mailu před vytvořením kteréhokoli účtu.</summary>
    /// <param name="settingKey">Klíč e-mailu člena nebo vyučujícího, který má být chybný.</param>
    /// <param name="email">Neplatná hodnota e-mailu nebo null pro chybějící údaj.</param>
    /// <returns>Úlohu dokončenou po ověření chyby konfigurace a absence účtů i rolí.</returns>
    [Theory]
    [InlineData(MemberEmailKey, null)]
    [InlineData(MemberEmailKey, "")]
    [InlineData(MemberEmailKey, "not-an-email")]
    [InlineData(TeacherEmailKey, null)]
    [InlineData(TeacherEmailKey, "")]
    [InlineData(TeacherEmailKey, "not-an-email")]
    public async Task Seed_RejectsInvalidEmailBeforeCreatingAccounts(string settingKey, string? email)
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: CreateSeedConfiguration());
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        configuration["DevelopmentSeed:Enabled"] = "true";
        configuration[settingKey] = email;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Contains(settingKey, exception.Message);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    /// <summary>Ověří odmítnutí shodných e-mailů po oříznutí mezer a bez ohledu na velikost písmen.</summary>
    /// <returns>Úlohu dokončenou po ověření chyby konfigurace bez částečně vytvořeného účtu.</returns>
    [Fact]
    public async Task Seed_RejectsMatchingEmailsBeforeCreatingAccounts()
    {
        var settings = CreateSeedConfiguration();
        settings[TeacherEmailKey] = $" {settings[MemberEmailKey]!.ToUpperInvariant()} ";
        await using var app = await RegistrationApp.CreateAsync(environment: "Development", seedEnabled: false, seedConfiguration: settings);
        app.Services.GetRequiredService<IConfiguration>()["DevelopmentSeed:Enabled"] = "true";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => DevelopmentAccountSeeder.SeedAsync(app.Services));

        Assert.Contains("must be different", exception.Message);
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    /// <summary>Ověří, že čisté vývojové spuštění bez vlastní konfigurace nevyžaduje přihlašovací údaje ani neseeduje.</summary>
    /// <returns>Úlohu dokončenou po startu API a kontrole prázdné tabulky účtů.</returns>
    [Fact]
    public async Task DevelopmentStartup_WithoutSeedConfigurationDoesNotCreateAccounts()
    {
        await using var app = await RegistrationApp.CreateAsync(environment: "Development");

        Assert.Equal(0L, await app.Database.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    /// <summary>Vytvoří náhodné údaje pouze pro izolovanou databázi daného testu; nečte lokální .env.</summary>
    /// <returns>Konfiguraci obou e-mailů a hesla pro předání testovacímu API.</returns>
    private static Dictionary<string, string?> CreateSeedConfiguration() => new()
    {
        [MemberEmailKey] = $"member-{Guid.NewGuid():N}@example.invalid",
        [TeacherEmailKey] = $"teacher-{Guid.NewGuid():N}@example.invalid",
        [PasswordKey] = Guid.NewGuid().ToString("N")
    };
}
