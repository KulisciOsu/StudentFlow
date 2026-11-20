using StudentFlow.Core.Authentication;

namespace StudentFlow.Core.Tests.Authentication;

public class RegistrationNameRulesTests
{
    [Theory]
    [InlineData("Jan")]
    [InlineData("Žofie")]
    [InlineData("Nováková")]
    [InlineData("Nova\u0301k")]
    [InlineData("किरण")]
    [InlineData("李")]
    public void SeparateName_AcceptsNonWhitespaceUnicode(string value) =>
        Assert.Null(RegistrationNameRules.Validate(value, "Jméno"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" Jan")]
    [InlineData("Jan ")]
    [InlineData("Jan Pavel")]
    [InlineData("Jan\tPavel")]
    [InlineData("Jan\nPavel")]
    [InlineData("Jan\u00A0Pavel")]
    [InlineData("Jan\u0085Pavel")]
    [InlineData("Jan\u2003Pavel")]
    [InlineData("Jan\u3000Pavel")]
    [InlineData("Jan\uFEFFPavel")]
    public void SeparateName_RejectsMissingOrWhitespace(string? value) =>
        Assert.NotNull(RegistrationNameRules.Validate(value, "Jméno"));

    [Theory]
    [InlineData("Jan1")]
    [InlineData("1Jan")]
    [InlineData("Jan١")]
    [InlineData("Jan１")]
    [InlineData("JanⅣ")]
    [InlineData("Novák-Svoboda")]
    [InlineData("O’Neill")]
    [InlineData("O'Neill")]
    [InlineData("Novák.")]
    [InlineData("Jan@")]
    [InlineData("Jan_")]
    [InlineData("Jan🦉")]
    [InlineData("Jan\u200B")]
    [InlineData("\u0301Jan")]
    [InlineData("A\u20DD")]
    public void SeparateName_RejectsDigitsAndNonLetterSymbols(string value) =>
        Assert.Equal("Jméno může obsahovat pouze písmena a diakritiku.", RegistrationNameRules.Validate(value, "Jméno"));

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    public void SeparateName_CountsUnicodeCodePoints(int length, bool valid) =>
        Assert.Equal(valid, RegistrationNameRules.Validate(string.Concat(Enumerable.Repeat("𐐀", length)), "Příjmení") is null);

    [Fact]
    public void SeparateName_ExplainsLimitOnlyWhenExceeded()
    {
        Assert.Equal("Vyplňte prosím pole „Jméno“.", RegistrationNameRules.Validate("", "Jméno"));
        Assert.Equal("Jméno může mít nejvýše 50 znaků.", RegistrationNameRules.Validate(new string('A', 51), "Jméno"));
    }
}
