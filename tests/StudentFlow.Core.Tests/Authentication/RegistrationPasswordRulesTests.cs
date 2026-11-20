using StudentFlow.Core.Authentication;

namespace StudentFlow.Core.Tests.Authentication;

public class RegistrationPasswordRulesTests
{
    [Theory]
    [InlineData("A1!abcde")]
    [InlineData("A1!abcdefghijkl")]
    [InlineData("ABCDEFG1!")]
    [InlineData("Žluťoučký1!")]
    [InlineData("A1!🦉🦉🦉🦉🦉")]
    public void ValidPasswords_AreAcceptedWithoutRequiringLowercase(string password)
        => Assert.Null(RegistrationPasswordRules.Validate(password));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A1!abcd")]
    [InlineData("A1!abcdefghijklm")]
    [InlineData("testheslo1!")]
    [InlineData("TestHeslo!!")]
    [InlineData("TestHeslo12")]
    [InlineData(" TestHeslo1!")]
    [InlineData("TestHeslo1! ")]
    [InlineData("Test\tHeslo1!")]
    [InlineData("Test\nHeslo1!")]
    [InlineData("Test\u00a0Heslo1!")]
    [InlineData("Test\u0085Heslo1!")]
    [InlineData("Test\ufeffHeslo1!")]
    public void InvalidPasswords_AreRejected(string? password)
        => Assert.NotNull(RegistrationPasswordRules.Validate(password));
}
