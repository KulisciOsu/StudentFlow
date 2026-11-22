using System.Globalization;
using System.Text;

namespace StudentFlow.Core.Authentication;

/// <summary>Pravidla pro nová hesla registrace a vývojových účtů; existující hesla se při přihlášení nemění.</summary>
public static class RegistrationPasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 15;

    /// <summary>Vrátí srozumitelnou chybu, nebo null; délku počítá v Unicode znacích a heslo neupravuje.</summary>
    public static string? Validate(string? password)
    {
        var characters = (password ?? "").EnumerateRunes().ToArray();
        if (characters.Length is < MinLength or > MaxLength)
            return "Heslo musí mít 8 až 15 znaků.";
        if (characters.Any(character => Rune.IsWhiteSpace(character) || character.Value == 0xFEFF))
            return "Heslo zadejte bez mezer, tabulátorů a odřádkování.";
        if (!characters.Any(Rune.IsUpper))
            return "Přidejte do hesla alespoň jedno velké písmeno.";
        if (!characters.Any(Rune.IsDigit))
            return "Přidejte do hesla alespoň jednu číslici.";
        if (!characters.Any(character => Rune.GetUnicodeCategory(character) is
            UnicodeCategory.ConnectorPunctuation or UnicodeCategory.DashPunctuation or
            UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation or UnicodeCategory.MathSymbol or
            UnicodeCategory.CurrencySymbol or UnicodeCategory.ModifierSymbol or UnicodeCategory.OtherSymbol))
            return "Přidejte do hesla alespoň jeden speciální znak, například !, ? nebo #.";
        return null;
    }
}
