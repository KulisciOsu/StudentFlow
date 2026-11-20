using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

/// <summary>Vstup registrace s právě jedním typem účtu; potvrzení hesla zůstává pouze ve frontendu.</summary>
public sealed class RegisterRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    /// <summary>Kód STUDENT pro člena, nebo TEACHER pro vyučujícího.</summary>
    public string? AccountType { get; init; }
    /// <summary>Musí být výslovně true; chybějící hodnota nepotvrzuje seznámení se zásadami.</summary>
    public bool PrivacyAccepted { get; init; }
    public string? PrivacyPolicyVersion { get; init; }

    /// <summary>Vrátí chyby pod názvy polí API; délky počítá v Unicode kódových bodech.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (RegistrationNameRules.Validate(FirstName, "Jméno") is { } firstNameError)
            errors["firstName"] = [firstNameError];
        if (RegistrationNameRules.Validate(LastName, "Příjmení") is { } lastNameError)
            errors["lastName"] = [lastNameError];
        AuthInputValidation.AddEmailErrors(errors, Email);
        if (RegistrationPasswordRules.Validate(Password) is { } passwordError)
            errors["password"] = [passwordError];
        if (AccountType is not ("STUDENT" or "TEACHER"))
            errors["accountType"] = ["Vyberte, jestli jste člen týmu, nebo vyučující."];
        if (!PrivacyAccepted)
            errors["privacyAccepted"] = ["Před vytvořením účtu potvrďte, že jste se seznámili se zásadami ochrany osobních údajů."];
        else if (PrivacyPolicyVersion != RegistrationPrivacyPolicy.Version)
            errors["privacyAccepted"] = ["Zásady se změnily. Obnovte stránku, přečtěte si aktuální znění a potvrďte ho znovu."];
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
