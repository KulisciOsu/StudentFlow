using Microsoft.AspNetCore.Identity;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Development;

/// <summary>Připraví známý účet člena pouze pro lokální vývoj; existující účet nikdy nepřepisuje.</summary>
public static class DevelopmentAccountSeeder
{
    private const string Email = "tester@studentflow.test";
    private const string Password = "StudentFlow-Test-2026!";

    /// <summary>Po migracích založí chybějící účet, pokud jde o Development a seed je povolený.</summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentSeed:Enabled")) return;

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        if (await accounts.FindByEmailAsync(Email, cancellationToken) is not null) return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
        var hash = hasher.HashPassword(new object(), Password);
        var result = await accounts.CreateAsync(new NewUserAccount("Testovací člen", Email, hash, AccountType.Member), cancellationToken);
        if (result.Status == CreateUserStatus.Created)
            services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DevelopmentAccountSeeder))
                .LogInformation("Vytvořen vývojový účet {Email} typu STUDENT.", Email);
    }
}
