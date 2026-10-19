# 🌊 StudentFlow

- Systém pro správu studentských týmových projektů.

## 👥 Tým

### Název týmu

- Kulíšci ♥

### Členové týmu a jejich responsibilita

```mermaid
flowchart LR
    subgraph TEAM["Členové týmu"]
        direction TB
        A["Adam Gebauer"]
        V["Vladimír Pospíšil"]
        M["Marek Szczepanik"]
        H["Honza Sebroň"]
        K["Kryštof Kristuf"]
    end

    subgraph RESPONSIBILITIES["Odpovědnosti"]
        direction TB
        LEAD["Vedení týmu"]
        DESIGN["Design"]
        QUALITY["Kvalita kódu"]
        BACKEND["Backend"]
        FRONTEND["Frontend"]
        DATABASE["Databáze"]
        TESTING["Testování"]
    end

    A --> LEAD
    A --> DESIGN
    V --> QUALITY
    M --> BACKEND
    M --> FRONTEND
    H --> DATABASE
    K --> TESTING

    classDef person fill:#1d4ed8,stroke:#1e3a8a,color:#ffffff,stroke-width:2px
    classDef responsibility fill:#334155,stroke:#64748b,color:#ffffff,stroke-width:1px

    class A,V,M,H,K person
    class LEAD,DESIGN,QUALITY,BACKEND,FRONTEND,DATABASE,TESTING responsibility

    style TEAM fill:transparent,stroke:#64748b,color:#64748b
    style RESPONSIBILITIES fill:transparent,stroke:#64748b,color:#64748b
```

> [!CAUTION]
> Všichni členové týmu si navzájem poskytují podporu. Vedoucí pozice slouží především k rozdělení odpovědností, koordinaci příslušných oblastí a jejich smysl. Neznamenají výhradní odpovědnost za danou oblast ani omezení zapojení ostatních členů týmu.

## 📚 Dokumentace

| FÁZE I - INCEPTION | FÁZE II - ELABORATION |
| :--- | :--- |
| [Vize projektu](documents/INCEPTION/VIZE_PROJEKTU.md) | [Návrh architektury](documents/ELABORATION/ARCHITECTURAL_DESIGN.md) |
| [Popis problému](documents/INCEPTION/PROBLEM_DESCRIPTION.md) | [Technologický návrh](documents/ELABORATION/TECHNOLOGICAL_DESIGN.md) |
| [Stakeholděři](documents/INCEPTION/STAKEHOLDERS.md) |  |
| [Risk-list – seznam rizik](documents/INCEPTION/RISK_LIST.md) |  |
| [Rozsah projektu – Scope](documents/INCEPTION/SCOPE_OF_PROJECT.md) |  |
| [Cíle projektu](documents/INCEPTION/PROJECT_OBJECTIVES.md) |  |
| [Use Case – případy užití](documents/INCEPTION/USE_CASE.md) |  |
| [Požadavky systému](documents/INCEPTION/REQUIREMENTS.md) |  |
| [Kandidát architektury](documents/INCEPTION/Kandidat-architektury.png) |  |

