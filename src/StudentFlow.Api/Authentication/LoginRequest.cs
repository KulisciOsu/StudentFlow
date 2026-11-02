namespace StudentFlow.Api.Authentication;

public sealed class LoginRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public bool RememberMe { get; init; }

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        AuthInputValidation.AddEmailAndPasswordErrors(errors, Email, Password);
        return errors;
    }
}
