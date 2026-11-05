using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

/// <summary>Vstup registrace s právě jedním typem účtu; potvrzení hesla zůstává pouze ve frontendu.</summary>
public sealed class RegisterRequest
{
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    /// <summary>Kód STUDENT pro člena, nebo TEACHER pro vyučujícího.</summary>
    public string? AccountType { get; init; }

    /// <summary>Vrátí chyby pod názvy polí API; délky počítá v Unicode kódových bodech.</summary>
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

    /// <summary>Převede typ účtu po úspěšné validaci požadavku.</summary>
    public AccountType GetAccountType() => AccountTypeCodes.FromCode(AccountType!);
}

/// <summary>Veřejné údaje účtu; číselné ID se přenáší jako řetězec kvůli přesnosti v JavaScriptu.</summary>
public sealed record AuthUserDto(string Id, string DisplayName, string Email, string AccountType)
{
    public static AuthUserDto From(UserIdentity user) => new(user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
        user.DisplayName, user.Email, AccountTypeCodes.ToCode(user.AccountType));
}
