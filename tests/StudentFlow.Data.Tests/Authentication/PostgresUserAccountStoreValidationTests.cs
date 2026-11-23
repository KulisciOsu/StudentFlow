using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.Tests.Authentication;

/// <summary>
/// Jednotkové testy kontrol vstupu <see cref="PostgresUserAccountStore"/>, které proběhnou bez databáze.
/// </summary>
public class PostgresUserAccountStoreValidationTests
{
    private readonly PostgresUserAccountStore store = new(UnreachableDatabase.Create());

    /// <summary>Vytvoří platný nový účet, ve kterém lze změnit jednotlivé údaje.</summary>
    private static NewUserAccount Account(string firstName = "Jan", string lastName = "Novák",
        string email = "jan.novak@example.test", string passwordHash = "hash") =>
        new(firstName, lastName, email, passwordHash, AccountType.Member);

    /// <summary>Chybějící účet se odmítne.</summary>
    [Fact]
    public async Task CreateAsync_NullAccount_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.CreateAsync(null!));
    }

    /// <summary>Jméno musí splnit stejná pravidla jako při registraci.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Jan1")]
    [InlineData("Jan Pavel")]
    [InlineData("Jan-Pavel")]
    public async Task CreateAsync_InvalidFirstName_Throws(string firstName)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(firstName: firstName)));
    }

    /// <summary>Příjmení musí splnit stejná pravidla jako při registraci, včetně délky nejvýše 50 znaků.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Novák\u00a0Svoboda")]
    [InlineData("Novák.")]
    public async Task CreateAsync_InvalidLastName_Throws(string lastName)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(lastName: lastName)));
    }

    /// <summary>Příjmení delší než 50 znaků se neořezává, ale odmítne.</summary>
    [Fact]
    public async Task CreateAsync_TooLongLastName_Throws()
    {
        var lastName = new string('a', RegistrationNameRules.MaxLength + 1);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(lastName: lastName)));
    }

    /// <summary>E-mail musí být vyplněný a mít 3 až 320 znaků.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a@")]
    public async Task CreateAsync_InvalidEmail_Throws(string email)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(email: email)));
    }

    /// <summary>E-mail delší než 320 znaků se odmítne.</summary>
    [Fact]
    public async Task CreateAsync_TooLongEmail_Throws()
    {
        var email = new string('a', 320) + "@example.test";

        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(email: email)));
    }

    /// <summary>Účet bez hashe hesla se neuloží.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateAsync_BlankPasswordHash_Throws(string passwordHash)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.CreateAsync(Account(passwordHash: passwordHash)));
    }

    /// <summary>Platný účet projde kontrolou vstupu a teprve potom se pokusí připojit k databázi.</summary>
    [Fact]
    public async Task CreateAsync_ValidAccount_ReachesDatabase()
    {
        await Assert.ThrowsAnyAsync<NpgsqlException>(() => store.CreateAsync(Account()));
    }

    /// <summary>Hledání podle prázdného e-mailu se odmítne.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task FindByEmailAsync_BlankEmail_Throws(string email)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.FindByEmailAsync(email));
    }

    /// <summary>Změna hesla vyžaduje očekávaný i nový hash.</summary>
    [Theory]
    [InlineData("", "new-hash")]
    [InlineData("old-hash", "")]
    [InlineData(" ", "new-hash")]
    public async Task UpdatePasswordHashAsync_BlankHash_Throws(string expectedHash, string newHash)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.UpdatePasswordHashAsync(1, expectedHash, newHash));
    }
}
