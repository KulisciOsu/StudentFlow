using Microsoft.AspNetCore.Identity;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Development;

/// <summary>Připraví účty člena a vyučujícího pouze pro lokální vývoj; existující účty nikdy nepřepisuje.</summary>
public static class DevelopmentAccountSeeder
{
    private const string Password = "StudentFlow-Test-2026!";
    private static readonly (string DisplayName, string Email, AccountType AccountType)[] SeedAccounts =
    [
        ("Testovací člen", "tester@studentflow.test", AccountType.Member),
        ("Testovací vyučující", "teacher@studentflow.test", AccountType.Teacher)
    ];

    /// <summary>Po migracích založí chybějící účty, pokud jde o Development a seed je povolený.</summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentSeed:Enabled")) return;

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
        foreach (var account in SeedAccounts)
        {
            if (await accounts.FindByEmailAsync(account.Email, cancellationToken) is not null) continue;

            var hash = hasher.HashPassword(new object(), Password);
            var result = await accounts.CreateAsync(new NewUserAccount(account.DisplayName, account.Email, hash, account.AccountType), cancellationToken);
            if (result.Status == CreateUserStatus.Created)
                services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DevelopmentAccountSeeder))
                    .LogInformation("Vytvořen vývojový účet {Email} typu {AccountType}.", account.Email, AccountTypeCodes.ToCode(account.AccountType));
        }
    }
}
