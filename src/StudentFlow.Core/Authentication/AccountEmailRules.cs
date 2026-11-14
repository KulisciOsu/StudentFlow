using System.Net.Mail;

namespace StudentFlow.Core.Authentication;

/// <summary>
/// Jediné pravidlo formátu e-mailu účtu; používá ho registrace, přihlášení i přidávání členů projektu.
/// </summary>
public static class AccountEmailRules
{
    /// <summary>Zpráva pro uživatele, když e-mail pravidlu neodpovídá.</summary>
    public const string InvalidMessage = "Zadejte platnou e-mailovou adresu, nejvýše 320 znaků.";

    /// <summary>
    /// Ověří už oříznutý e-mail: 3 až 320 Unicode znaků, bez mezer, právě jeden zavináč
    /// a adresa, kterou .NET přečte beze změny (odmítne například tvar „Jméno &lt;adresa&gt;“).
    /// </summary>
    /// <param name="email">E-mail po odstranění okolních mezer.</param>
    /// <returns>True, pokud e-mail pravidlu odpovídá.</returns>
    public static bool IsValid(string email) =>
        email.EnumerateRunes().Count() is >= 3 and <= 320
        && !email.Any(char.IsWhiteSpace)
        && email.Count(c => c == '@') == 1
        && MailAddress.TryCreate(email, out var address)
        && address.Address == email;
}
