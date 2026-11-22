using Npgsql;
using StudentFlow.Data.ProjectMembers;

namespace StudentFlow.Data.Tests.ProjectMembers;

/// <summary>
/// Jednotkové testy kontrol vstupu <see cref="PostgresProjectMemberStore"/>, které proběhnou bez databáze.
/// </summary>
public class PostgresProjectMemberStoreValidationTests
{
    private readonly PostgresProjectMemberStore store = new(UnreachableDatabase.Create());

    /// <summary>Účet nového člena nelze hledat podle prázdného e-mailu.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindAccountByEmailAsync_BlankEmail_Throws(string email)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.FindAccountByEmailAsync(email));
    }

    /// <summary>Vyplněný e-mail projde kontrolou a teprve potom se pokusí připojit k databázi.</summary>
    [Fact]
    public async Task FindAccountByEmailAsync_Email_ReachesDatabase()
    {
        await Assert.ThrowsAnyAsync<NpgsqlException>(() => store.FindAccountByEmailAsync("jan.novak@example.test"));
    }
}
