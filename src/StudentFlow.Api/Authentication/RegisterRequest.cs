using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

public sealed class RegisterRequest
{
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? AccountType { get; init; }

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        var name = DisplayName?.Trim() ?? "";
        if (name.EnumerateRunes().Count() is < 1 or > 160)
            errors["displayName"] = ["Zadejte jméno a příjmení o délce 1 až 160 znaků."];
        AuthInputValidation.AddEmailAndPasswordErrors(errors, Email, Password);
        if (AccountType is not ("STUDENT" or "TEACHER"))
            errors["accountType"] = ["Vyberte typ účtu: Člen, nebo Vyučující."];
        return errors;
    }

    public AccountType GetAccountType() => AccountTypeCodes.FromCode(AccountType!);
}

public sealed record AuthUserDto(string Id, string DisplayName, string Email, string AccountType)
{
    public static AuthUserDto From(UserIdentity user) => new(user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
        user.DisplayName, user.Email, AccountTypeCodes.ToCode(user.AccountType));
}
