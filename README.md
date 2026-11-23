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

        A ~~~ V
        V ~~~ M
        M ~~~ H
        H ~~~ K
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

    classDef person fill:#e2e8f0,stroke:#a8b5c5,color:#334155,stroke-width:1px
    classDef responsibility fill:#f1f5f9,stroke:#cbd5e1,color:#475569,stroke-width:1px

    class A,V,M,H,K person
    class LEAD,DESIGN,QUALITY,BACKEND,FRONTEND,DATABASE,TESTING responsibility

    style TEAM fill:transparent,stroke:#cbd5e1,color:#64748b
    style RESPONSIBILITIES fill:transparent,stroke:#cbd5e1,color:#64748b

    linkStyle default stroke:#94a3b8,stroke-width:1px
```

> [!CAUTION]
> Všichni členové týmu si navzájem poskytují podporu. Vedoucí pozice slouží především k rozdělení odpovědností, koordinaci příslušných oblastí a jejich smysl. Neznamenají výhradní odpovědnost za danou oblast ani omezení zapojení ostatních členů týmu.

## 📚 Dokumentace

Dokumentace postupuje podle zadání předmětu: **problém → vize → uživatelé → požadavky → případy užití → návrh → architektura → prototyp**. Každý dokument uvádí, na který předchozí navazuje.

| FÁZE I - INCEPTION | FÁZE II - ELABORATION |
| :--- | :--- |
| [Popis problému](documents/INCEPTION/PROBLEM_DESCRIPTION.md) | [Wireframy](documents/ELABORATION/Wireframe.pdf) |
| [Vize projektu](documents/INCEPTION/VIZE_PROJEKTU.md) | [ER model](documents/ELABORATION/StudentFlow_ER.svg) |
| [Rozsah projektu – Scope](documents/INCEPTION/SCOPE_OF_PROJECT.md) | [Autentizace – API a doplňky wireframů](documents/ELABORATION/auth/README.md) |
| [Cíle projektu](documents/INCEPTION/PROJECT_OBJECTIVES.md) | [Členové projektu](documents/ELABORATION/projects/MEMBERS.md) |
| [Stakeholdeři](documents/INCEPTION/STAKEHOLDERS.md) | [Návrh architektury](documents/ELABORATION/ARCHITECTURAL_DESIGN.md) |
| [Požadavky systému](documents/INCEPTION/REQUIREMENTS.md) | [Technologický návrh](documents/ELABORATION/TECHNOLOGICAL_DESIGN.md) |
| [Use Case – případy užití](documents/INCEPTION/USE_CASE.md) | [Prototyp: 20 % implementace a ověření architektury](documents/ELABORATION/PROTOTYP.md) |
| [Risk-list – seznam rizik](documents/INCEPTION/RISK_LIST.md) | [Aktualizovaný plán projektu](documents/ELABORATION/PLAN_PROJEKTU.md) |
| [Kandidát architektury](documents/INCEPTION/Kandidat-architektury.png) | [Rozdělení práce v týmu](documents/ELABORATION/ROZDELENI_PRACE.md) |

## 🧪 Testování

[Pravidla testování, aktuální stav a spuštění testů](documents/TESTING.md).

## Lokální spuštění

[Společné vývojové prostředí: HTTPS, API, frontend a PostgreSQL](documents/DEVELOPMENT.md).
