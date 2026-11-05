using System.Net.Mail;

namespace StudentFlow.Api.Authentication;

/// <summary>Sdílená pravidla e-mailu a hesla pro registraci i přihlášení.</summary>
internal static class AuthInputValidation
{
    /// <summary>Doplní chyby polí; délky počítá v Unicode kódových bodech a heslo neořezává.</summary>
    public static void AddEmailAndPasswordErrors(Dictionary<string, string[]> errors, string? rawEmail, string? password)
    {
        var email = rawEmail?.Trim() ?? "";
        if (email.EnumerateRunes().Count() is < 3 or > 320 || email.Any(char.IsWhiteSpace)
            || email.Count(c => c == '@') != 1 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
            errors["email"] = ["Zadejte platnou e-mailovou adresu, nejvýše 320 znaků."];
        if ((password ?? "").EnumerateRunes().Count() is < 15 or > 128)
            errors["password"] = ["Heslo musí mít 15 až 128 znaků."];
    }
}
