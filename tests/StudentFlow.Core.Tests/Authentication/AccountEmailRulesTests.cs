using StudentFlow.Core.Authentication;

namespace StudentFlow.Core.Tests.Authentication;

/// <summary>
/// Jednotkové testy společného pravidla formátu e-mailu pro registraci, přihlášení i členy projektu.
/// </summary>
public class AccountEmailRulesTests
{
    /// <summary>
    /// Běžné platné adresy pravidlo přijme.
    /// </summary>
    [Theory]
    [InlineData("jan@example.test")]
    [InlineData("jan.novak+tym@example.test")]
    [InlineData("a@b")]
    public void IsValid_RegularAddress_ReturnsTrue(string email)
    {
        Assert.True(AccountEmailRules.IsValid(email));
    }

    /// <summary>
    /// Prázdné, krátké a jinak chybné adresy pravidlo odmítne; okolní mezery už musí být oříznuté.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("a@")]
    [InlineData("tomas.example.test")]
    [InlineData("tomas@@example.test")]
    [InlineData("tomas dvorak@example.test")]
    [InlineData(" jan@example.test")]
    [InlineData("@example.test")]
    [InlineData("jan@")]
    [InlineData("Jan Novák <jan@example.test>")]
    public void IsValid_MalformedAddress_ReturnsFalse(string email)
    {
        Assert.False(AccountEmailRules.IsValid(email));
    }

    /// <summary>
    /// Nejdelší povolená adresa má 320 znaků.
    /// </summary>
    [Theory]
    [InlineData(320, true)]
    [InlineData(321, false)]
    public void IsValid_LengthBoundary_AllowsAtMost320Characters(int length, bool expected)
    {
        const string domain = "@example.test";
        var email = new string('a', length - domain.Length) + domain;

        Assert.Equal(expected, AccountEmailRules.IsValid(email));
    }
}
