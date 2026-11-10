using Microsoft.AspNetCore.Identity;
using Npgsql;
using StudentFlow.Api.Authentication;
using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Development;

/// <summary>
/// Připravuje účty člena a vyučujícího pro lokální vývoj přes stejné datové operace jako registrace.
/// </summary>
/// <remarks>
/// Vyžaduje databázi s provedenými migracemi. Existující účty podle e-mailu ponechává beze změny;
/// e-maily i heslo čte ze sekce DevelopmentSeed a heslo hashuje samostatně pro každý účet.
/// </remarks>
public static class DevelopmentAccountSeeder
{
    /// <summary>
    /// Založí chybějící testovací účty pouze v prostředí Development s povoleným DevelopmentSeed:Enabled.
    /// </summary>
    /// <param name="services">Kořenový poskytovatel služeb aplikace; pro datové operace se vytvoří vlastní scope.</param>
    /// <param name="cancellationToken">Token pro zrušení vyhledávání a vytváření účtů v databázi.</param>
    /// <returns>Úlohu dokončenou po vytvoření chybějících účtů nebo po přeskočení zakázaného seedu.</returns>
    /// <exception cref="InvalidOperationException">
    /// Seed je povolený ve vývoji, ale e-maily nebo heslo v sekci DevelopmentSeed chybí či nejsou platné,
    /// nebo mají oba typy účtu shodný e-mail. Heslo musí mít 15 až 128 znaků Unicode.
    /// </exception>
    /// <exception cref="OperationCanceledException">Databázová operace byla zrušena předaným tokenem.</exception>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var configuration = services.GetRequiredService<IConfiguration>();
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentSeed:Enabled")) return;

        var password = configuration["DevelopmentSeed:Password"] ?? "";
        if (password.EnumerateRunes().Count() is < 15 or > 128)
            throw new InvalidOperationException("DevelopmentSeed:Password must contain 15 to 128 Unicode characters.");

        var memberEmail = configuration["DevelopmentSeed:MemberEmail"]?.Trim() ?? "";
        var teacherEmail = configuration["DevelopmentSeed:TeacherEmail"]?.Trim() ?? "";
        var leaderEmail = configuration["DevelopmentSeed:LeaderEmail"]?.Trim() ?? "";
        var seedAccounts = new List<(string DisplayName, string Email, AccountType AccountType)>
        {
            (DisplayName: "Testovací člen", Email: memberEmail, AccountType: AccountType.Member),
            (DisplayName: "Testovací vyučující", Email: teacherEmail, AccountType: AccountType.Teacher)
        };
        // Vedoucí je volitelný člen, kterého seed nástěnky nastaví jako vedoucího ukázkového projektu.
        if (leaderEmail.Length != 0) seedAccounts.Add((DisplayName: "Testovací vedoucí", Email: leaderEmail, AccountType: AccountType.Member));
        foreach (var account in seedAccounts)
        {
            var errors = new Dictionary<string, string[]>();
            AuthInputValidation.AddEmailAndPasswordErrors(errors, account.Email, password);
            if (errors.Count != 0)
                throw new InvalidOperationException("DevelopmentSeed:MemberEmail and DevelopmentSeed:TeacherEmail must be valid email addresses.");
        }
        if (string.Equals(memberEmail, teacherEmail, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("DevelopmentSeed:MemberEmail and DevelopmentSeed:TeacherEmail must be different.");
        if (seedAccounts.Select(account => account.Email.ToUpperInvariant()).Distinct().Count() != seedAccounts.Count)
            throw new InvalidOperationException("DevelopmentSeed:LeaderEmail must be different from the member and teacher emails.");

        await using var scope = services.CreateAsyncScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<object>>();
        foreach (var account in seedAccounts)
        {
            if (await accounts.FindByEmailAsync(account.Email, cancellationToken) is not null) continue;

            var hash = hasher.HashPassword(new object(), password);
            var result = await accounts.CreateAsync(new NewUserAccount(account.DisplayName, account.Email, hash, account.AccountType), cancellationToken);
            if (result.Status == CreateUserStatus.Created)
                services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DevelopmentAccountSeeder))
                    .LogInformation("Vytvořen vývojový účet {Email} typu {AccountType}.", account.Email, AccountTypeCodes.ToCode(account.AccountType));
        }

        await DevelopmentBoardSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>(), memberEmail, leaderEmail, cancellationToken);
    }
}
