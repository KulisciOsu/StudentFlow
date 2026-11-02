using System.Text.Json.Serialization;

namespace StudentFlow.Core.Authentication;

public sealed record UserIdentity(long Id, string DisplayName, string Email, AccountType AccountType, bool IsActive);

// Oddělený interní výsledek pro ověření hesla; API vrací pouze veřejnou identitu.
// Třída záměrně nemá record ToString, který by vypisoval hash.
public sealed class UserCredentials(UserIdentity user, string passwordHash)
{
    public UserIdentity User { get; } = user;
    [JsonIgnore]
    public string PasswordHash { get; } = passwordHash;
}

public sealed class NewUserAccount(string displayName, string email, string passwordHash, AccountType accountType)
{
    public string DisplayName { get; } = displayName;
    public string Email { get; } = email;
    [JsonIgnore]
    public string PasswordHash { get; } = passwordHash;
    public AccountType AccountType { get; } = accountType;
}

public enum CreateUserStatus { Created, EmailAlreadyRegistered }

public sealed record CreateUserResult(CreateUserStatus Status, UserIdentity? User);

public interface IUserAccountStore
{
    Task<CreateUserResult> CreateAsync(NewUserAccount account, CancellationToken cancellationToken = default);
    Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserIdentity?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRoleCodesAsync(long id, CancellationToken cancellationToken = default);
    // Aktualizace při rehashi nesmí přepsat souběžnou změnu hesla.
    Task<bool> UpdatePasswordHashAsync(long id, string expectedHash, string newHash, CancellationToken cancellationToken = default);
}
