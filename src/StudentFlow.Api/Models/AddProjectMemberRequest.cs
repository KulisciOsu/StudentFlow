namespace StudentFlow.Api.Models;

/// <summary>
/// Vstup přidání člena; jiná pole API odmítne, takže klient nemůže nastavit například vedení.
/// </summary>
public sealed class AddProjectMemberRequest
{
    /// <summary>E-mail registrovaného studenta; okolní mezery se ignorují.</summary>
    public string? Email { get; init; }
}
