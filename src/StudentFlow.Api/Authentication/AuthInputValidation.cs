using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

/// <summary>Validace e-mailu a vstupu přihlášení; nová hesla mají samostatná registrační pravidla.</summary>
internal static class AuthInputValidation
{
    /// <summary>Doplní chyby polí; délky počítá v Unicode kódových bodech a heslo neořezává.</summary>
    public static void AddEmailErrors(Dictionary<string, string[]> errors, string? rawEmail)
    {
        var email = rawEmail?.Trim() ?? "";
        if (!AccountEmailRules.IsValid(email))
            errors["email"] = [AccountEmailRules.InvalidMessage];
    }

    /// <summary>Připustí i dříve registrovaná hesla; jejich správnost následně ověří hash.</summary>
    public static void AddLoginPasswordErrors(Dictionary<string, string[]> errors, string? password)
    {
        if (string.IsNullOrEmpty(password)) errors["password"] = ["Zadejte heslo."];
        else if (password.EnumerateRunes().Count() > 128) errors["password"] = ["Heslo může mít nejvýše 128 znaků."];
    }
}
