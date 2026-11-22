using System.Globalization;
using System.Text;

namespace StudentFlow.Core.Authentication;

/// <summary>Pravidla pro samostatné jméno a příjmení; hodnoty se neořezávají ani nerozdělují.</summary>
public static class RegistrationNameRules
{
    public const int MaxLength = 50;

    public static string? Validate(string? value, string label)
    {
        var characters = (value ?? "").EnumerateRunes().ToArray();
        if (characters.Length == 0)
            return $"Vyplňte prosím pole „{label}“.";
        if (characters.Length > MaxLength)
            return $"{label} může mít nejvýše {MaxLength} znaků.";
        if (characters.Any(character => Rune.IsWhiteSpace(character) || character.Value == 0xFEFF))
            return $"{label} zadejte bez mezer, tabulátorů a odřádkování.";
        if (!Rune.IsLetter(characters[0]) || characters.Any(character =>
            !Rune.IsLetter(character) && Rune.GetUnicodeCategory(character) is not
                (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)))
            return $"{label} může obsahovat pouze písmena a diakritiku.";
        return null;
    }
}
