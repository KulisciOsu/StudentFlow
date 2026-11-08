namespace StudentFlow.Core.ProjectMembers;

/// <summary>Výsledek načtení seznamu členů projektu.</summary>
public enum ListMembersStatus
{
    /// <summary>Volající je členem projektu a seznam je načtený.</summary>
    Listed,
    /// <summary>Projekt neexistuje, nebo volající není jeho členem; navenek se tyto situace nerozlišují.</summary>
    ProjectNotFound
}

/// <summary>Seznam členů projektu; při ProjectNotFound je prázdný.</summary>
public sealed record ListMembersResult(ListMembersStatus Status, IReadOnlyList<ProjectMember> Members);

/// <summary>Výsledek přidání člena podle e-mailu.</summary>
public enum AddMemberStatus
{
    /// <summary>Účet byl přidán jako běžný člen.</summary>
    Added,
    /// <summary>E-mail je prázdný nebo nemá platný formát.</summary>
    InvalidEmail,
    /// <summary>Projekt neexistuje, nebo volající není jeho členem.</summary>
    ProjectNotFound,
    /// <summary>Volající je členem projektu, ale není jeho vedoucím.</summary>
    NotLeader,
    /// <summary>Aktivní účet s tímto e-mailem neexistuje.</summary>
    AccountNotFound,
    /// <summary>Účet existuje, ale není typu STUDENT; vyučující získávají přístup jinak (UC09).</summary>
    AccountNotStudent,
    /// <summary>Účet už je členem projektu; nové členství nevzniklo.</summary>
    AlreadyMember
}

/// <summary>Výsledek přidání člena; Member je vyplněný pouze při Added.</summary>
public sealed record AddMemberResult(AddMemberStatus Status, ProjectMember? Member);

/// <summary>Výsledek odebrání člena z projektu.</summary>
public enum RemoveMemberStatus
{
    /// <summary>Členství bylo odebráno; příspěvky člena zůstávají zachované.</summary>
    Removed,
    /// <summary>Projekt neexistuje, nebo volající není jeho členem.</summary>
    ProjectNotFound,
    /// <summary>Volající je členem projektu, ale není jeho vedoucím.</summary>
    NotLeader,
    /// <summary>Odebíraný uživatel není členem projektu.</summary>
    MemberNotFound,
    /// <summary>Vedoucího nelze odebrat; projekt musí mít vždy vedoucího.</summary>
    LeaderCannotBeRemoved
}
