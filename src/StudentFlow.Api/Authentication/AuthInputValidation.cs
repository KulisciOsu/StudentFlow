using StudentFlow.Core.Authentication;

namespace StudentFlow.Api.Authentication;

/// <summary>Sdílená pravidla e-mailu a hesla pro registraci i přihlášení.</summary>
internal static class AuthInputValidation
{
    /// <summary>Doplní chyby polí; délky počítá v Unicode kódových bodech a heslo neořezává.</summary>
    public static void AddEmailAndPasswordErrors(Dictionary<string, string[]> errors, string? rawEmail, string? password)
    {
        var email = rawEmail?.Trim() ?? "";
        if (!AccountEmailRules.IsValid(email))
            errors["email"] = [AccountEmailRules.InvalidMessage];
        if ((password ?? "").EnumerateRunes().Count() is < 15 or > 128)
            errors["password"] = ["Heslo musí mít 15 až 128 znaků."];
    }
}
