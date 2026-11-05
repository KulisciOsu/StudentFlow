namespace StudentFlow.Api.Authentication;

/// <summary>Vstup přihlášení; heslo se před ověřením nijak neupravuje.</summary>
public sealed class LoginRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    /// <summary>Volí session na 30 dní s trvalou cookie místo session na 8 hodin bez trvalé cookie.</summary>
    public bool RememberMe { get; init; }

    /// <summary>Vrátí chyby pod názvy polí API; prázdný slovník znamená platný vstup.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        AuthInputValidation.AddEmailAndPasswordErrors(errors, Email, Password);
        return errors;
    }
}
