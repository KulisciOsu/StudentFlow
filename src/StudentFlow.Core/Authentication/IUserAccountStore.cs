using System.Text.Json.Serialization;

namespace StudentFlow.Core.Authentication;

/// <summary>Identita účtu bez přihlašovacích tajemství, včetně aktuální aktivity a typu účtu.</summary>
public sealed record UserIdentity(long Id, string DisplayName, string Email, AccountType AccountType, bool IsActive);

/// <summary>Interní podklady pro ověření hesla; do odpovědi API patří pouze identita.</summary>
/// <remarks>Jde o třídu, aby automatický výpis recordu nezahrnoval hash hesla.</remarks>
public sealed class UserCredentials(UserIdentity user, string passwordHash)
{
    public UserIdentity User { get; } = user;
    /// <summary>Uložený hash pro ověření hesla; nesmí se logovat ani vracet klientovi.</summary>
    [JsonIgnore]
    public string PasswordHash { get; } = passwordHash;
}

/// <summary>Podklady pro založení účtu s již zahashovaným heslem a právě jedním typem účtu.</summary>
public sealed class NewUserAccount(string displayName, string email, string passwordHash, AccountType accountType)
{
    public string DisplayName { get; } = displayName;
    public string Email { get; } = email;
    [JsonIgnore]
    public string PasswordHash { get; } = passwordHash;
    public AccountType AccountType { get; } = accountType;
}

public enum CreateUserStatus { Created, EmailAlreadyRegistered }

/// <summary>Výsledek založení účtu; při duplicitním e-mailu je User null.</summary>
public sealed record CreateUserResult(CreateUserStatus Status, UserIdentity? User);

/// <summary>Ukládání účtů a přihlašovacích údajů nezávislé na HTTP vrstvě.</summary>
public interface IUserAccountStore
{
    /// <summary>Atomicky založí účet i jeho typ; kolizi normalizovaného e-mailu vrátí jako výsledek.</summary>
    Task<CreateUserResult> CreateAsync(NewUserAccount account, CancellationToken cancellationToken = default);
    /// <summary>Vyhledá účet včetně hashe podle normalizovaného e-mailu, nebo vrátí null.</summary>
    Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    /// <summary>Vrátí identitu existujícího účtu bez hashe, včetně příznaku aktivity, nebo null.</summary>
    Task<UserIdentity?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    /// <summary>Vrátí kódy rolí přidělených účtu; neexistujícímu účtu odpovídá prázdný seznam.</summary>
    Task<IReadOnlyList<string>> GetRoleCodesAsync(long id, CancellationToken cancellationToken = default);
    /// <summary>Nahradí hash jen při shodě s očekávaným, aby rehash nepřepsal souběžnou změnu hesla.</summary>
    /// <returns>True, pokud byl hash změněn; jinak false.</returns>
    Task<bool> UpdatePasswordHashAsync(long id, string expectedHash, string newHash, CancellationToken cancellationToken = default);
}
