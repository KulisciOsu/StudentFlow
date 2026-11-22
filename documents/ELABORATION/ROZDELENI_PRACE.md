# Rozdělení práce v týmu

> Výstup fází Inception (složení týmu a rozdělení rolí) a Elaboration (rozdělení práce). Navazuje na [plán projektu](PLAN_PROJEKTU.md).

## Tým Kulíšci

| Člen | Oblast odpovědnosti |
| --- | --- |
| Adam Gebauer | Vedení týmu, design |
| Vladimír Pospíšil | Kvalita kódu |
| Marek Szczepanik | Backend, frontend |
| Honza Sebroň | Databáze |
| Kryštof Kristuf | Testování |

Vedoucí oblasti koordinuje svou oblast a kontroluje výstupy ostatních (riziko R9). Neznamená to, že na oblasti pracuje sám: všichni členové si navzájem pomáhají a pracují napříč oblastmi.

## Princip: každý člen staví celou funkční část

Pro implementaci si tým rozdělil práci podle funkčních částí systému (epiků v Jiře), ne podle vrstev. Každý člen dodal jednu funkční část celou: wireframe nebo návrh, frontend, backend, databázi a testy. Díky tomu:

- každý člen prakticky pracoval se všemi vrstvami architektury a je zastupitelný (zadání kap. 10),
- funkční části se propojují přes dohodnutá rozhraní REST API, takže integrace ověřuje architekturu (riziko R6),
- přínos každého člena je dohledatelný v Jiře i v Gitu podle klíče úkolu v názvu větve a commitu.

| Člen | Funkční část (epik) | Případ užití | Dílčí úkoly v Jiře |
| --- | --- | --- | --- |
| Kryštof Kristuf | Registrace, přihlášení a session (KAN-46) | UC12 | KAN-100 až KAN-117, KAN-131 až KAN-133 |
| Honza Sebroň | Přehled projektů a úkolů člena (KAN-47) | UC06 | KAN-80 až KAN-84 |
| Vladimír Pospíšil | Založení projektu a týmu (KAN-48) | UC04 | KAN-86 až KAN-89 |
| Adam Gebauer | Členové projektu (KAN-50) | UC13 | KAN-90 až KAN-94 |
| Marek Szczepanik | Kanban nástěnka a oprávnění na ní | UC01, UC06 | KAN-96 až KAN-99, KAN-122 |

Stav jednotlivých částí je v dokumentu [prototyp](PROTOTYP.md).

## Autorství dokumentace

Některé dokumenty nahrál do repozitáře jiný člen, než který je psal, proto historie Gitu sama autorství nedokládá. Tabulka uvádí hlavního autora.

| Dokument | Hlavní autor |
| --- | --- |
| [Vize](../INCEPTION/VIZE_PROJEKTU.md), [popis problému](../INCEPTION/PROBLEM_DESCRIPTION.md), [stakeholdeři](../INCEPTION/STAKEHOLDERS.md), [rozsah](../INCEPTION/SCOPE_OF_PROJECT.md), [cíle](../INCEPTION/PROJECT_OBJECTIVES.md) | Adam Gebauer |
| [Risk-list](../INCEPTION/RISK_LIST.md), první verze | Adam Gebauer podle podkladů týmu |
| [Risk-list](../INCEPTION/RISK_LIST.md), aktualizace v Elaboration | Kryštof Kristuf |
| [Případy užití](../INCEPTION/USE_CASE.md) | Honza Sebroň |
| [Kandidát architektury](../INCEPTION/Kandidat-architektury.png) | Adam Gebauer |
| [Návrh architektury](ARCHITECTURAL_DESIGN.md), [technologický návrh](TECHNOLOGICAL_DESIGN.md) | Vladimír Pospíšil, aktualizace Marek Szczepanik |
| [Wireframy](Wireframe.pdf) | Adam Gebauer |
| [Návrh autentizace](auth/README.md) včetně doplňků wireframů | Kryštof Kristuf |
| [ER model](StudentFlow_ER.svg) | Kryštof Kristuf |
| [Členové projektu](projects/MEMBERS.md) | Adam Gebauer |
| [Vývojové prostředí](../DEVELOPMENT.md), [testování](../TESTING.md) | Kryštof Kristuf |
| [Prototyp](PROTOTYP.md), [plán projektu](PLAN_PROJEKTU.md), toto rozdělení práce | Adam Gebauer |

## Jak tým spolupracuje

- **Jira (prostor Kulíšci):** epiky a dílčí úkoly. Větve a commity k úkolům z Jiry obsahují klíč úkolu, takže Jira ukazuje související kód. Průběžné úpravy kvality celého projektu (například revize dokumentace) se do Jiry nezapisují.
- **GitHub:** jedna větev na úkol, sloučení do `main` přes pull request se schválením od dalších členů. CI spouští build a testy u každého pull requestu. Pull requesty se slučují merge commitem, aby zůstala vidět postupná práce každého člena.
- **Discord:** pravidelné schůzky týmu a hlášení chyb při společném testování.

## Příští semestr

V letním semestru zůstane v týmu jeden člen (riziko R2). Předání proto stojí na třech věcech: každý člen dokumentuje svou funkční část (například [členové projektu](projects/MEMBERS.md) nebo [návrh autentizace](auth/README.md)), aplikace jde spustit podle [vývojového prostředí](../DEVELOPMENT.md) a před koncem semestru proběhne předávací schůzka, na které zůstávající člen aplikaci sám spustí (cíl C10). Postup je v [plánu projektu](PLAN_PROJEKTU.md).
