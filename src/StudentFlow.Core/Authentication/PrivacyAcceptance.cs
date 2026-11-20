namespace StudentFlow.Core.Authentication;

/// <summary>Serverem zaznamenané potvrzení zásad při registraci; čas neurčuje klient.</summary>
public sealed record PrivacyAcceptance(string Version, DateTimeOffset AcceptedAt);

/// <summary>Verze textu poskytovaného registračním formulářem.</summary>
public static class RegistrationPrivacyPolicy
{
    public const string Version = "2026-11-20.2";
}
