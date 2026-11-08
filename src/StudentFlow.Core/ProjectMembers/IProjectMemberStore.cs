using StudentFlow.Core.Authentication;

namespace StudentFlow.Core.ProjectMembers;

/// <summary>Ukládání členství v projektech nezávislé na HTTP vrstvě; o oprávnění rozhodují pravidla členství.</summary>
/// <remarks>Zápisy navíc samy ověřují, že volající je v okamžiku zápisu vedoucím projektu.</remarks>
public interface IProjectMemberStore
{
    /// <summary>Vrátí členství uživatele v projektu, nebo null, pokud projekt neexistuje či uživatel není jeho členem.</summary>
    Task<ProjectMember?> FindMemberAsync(int projectId, long userId, CancellationToken cancellationToken = default);

    /// <summary>Vrátí aktuální členy projektu: nejprve vedoucího, potom ostatní podle jména.</summary>
    Task<IReadOnlyList<ProjectMember>> ListAsync(int projectId, CancellationToken cancellationToken = default);

    /// <summary>Vyhledá účet podle normalizovaného e-mailu včetně typu a příznaku aktivity, nebo vrátí null.</summary>
    /// <remarks>Nevrací hash hesla; slouží pouze k určení přidávaného člena.</remarks>
    Task<UserIdentity?> FindAccountByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Přidá účet jako běžného člena, pouze pokud je addedByUserId v okamžiku zápisu vedoucím projektu.</summary>
    /// <returns>Added s novým členstvím; AlreadyMember při existujícím členství; NotLeader, pokud podmínka vedoucího již neplatí.</returns>
    Task<AddMemberResult> AddAsync(int projectId, long userId, long addedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Odebere běžného člena, pouze pokud je removedByUserId v okamžiku zápisu vedoucím projektu.</summary>
    /// <remarks>Vedoucího nikdy neodebere. Úkoly, komentáře a další příspěvky člena nemění.</remarks>
    /// <returns>True, pokud bylo členství odebráno; jinak false.</returns>
    Task<bool> RemoveAsync(int projectId, long userId, long removedByUserId, CancellationToken cancellationToken = default);
}
