# Testování StudentFlow

Funkci implementuje jiný člen týmu. Po jejím předání píše testy vedoucí
testování. Autor implementace opravuje
nalezené chyby a vedoucí testování následně ověřuje opravu.

Tento dokument stanovuje pravidla pro další vývoj. Tabulka rozlišuje již
zavedené části od těch, které je potřeba implementovat.

## Úrovně testů

| Úroveň | Co ověřuje | Nástroj | Aktuální stav |
| --- | --- | --- | --- |
| Jednotkové backendové | Doménová pravidla, validace a výpočty bez infrastruktury. | C#, xUnit | Tři testovací projekty; dva testy controllerů a dva prázdné testy. Doménová logika zatím chybí. |
| Jednotkové a komponentové frontendové | Přesuny a pořadí úkolů, zpracování odpovědí a chování komponent. | TypeScript, navržený Vitest | Není zavedeno. |
| Integrační API | HTTP routování, stavové kódy, serializace, validace a oprávnění. | xUnit, WebApplicationFactory | Není zavedeno; přímé volání controlleru HTTP pipeline neověřuje. |
| Integrační databázové | Dotazy, zápisy, omezení a migrace na stejném databázovém systému jako produkce. | xUnit, navržený Testcontainers s PostgreSQL | Není zavedeno; datová vrstva zatím není implementovaná. |
| E2E | Kritické uživatelské průchody přes prohlížeč, frontend a API. | TypeScript, zvolený Playwright | Není zavedeno. |

Vybírej nejnižší úroveň, která spolehlivě ověří dané riziko. E2E sada pokrývá
kritické průchody; kombinace vstupů a okrajové případy ověřuj převážně v rychlejších
testech. Databázové integrační testy používají izolovanou testovací instanci
PostgreSQL; průchod proti náhradní databázi sám nepotvrzuje kompatibilitu s PostgreSQL.

## Funkční oblasti

Podklady specifické pro jednotlivé oblasti patří do `documents/TESTING/areas/`.
Spustitelné testy zůstávají v příslušných testovacích projektech podle úrovně
a technologie; soubory v těchto složkách dokumentace se automaticky nespouštějí.
Prázdné složky obsahují `.gitkeep`, aby se zachovaly v Gitu.

| Oblast | Složka |
| --- | --- |
| Uživatelé a přihlášení | [auth](TESTING/areas/auth/) |
| Role a oprávnění | [roles](TESTING/areas/roles/) |
| Projekty | [projects](TESTING/areas/projects/) |
| Týmy | [teams](TESTING/areas/teams/) |
| Úkoly a přehled vlastní práce | [tasks](TESTING/areas/tasks/) |
| Kanban nástěnka | [kanban](TESTING/areas/kanban/) |
| Komentáře | [comments](TESTING/areas/comments/) |
| Soubory a přílohy | [files](TESTING/areas/files/) |
| Statistiky členů | [statistics](TESTING/areas/statistics/) |
| Přehled pro vyučujícího | [teacher](TESTING/areas/teacher/) |

## Pravidla psaní testů

- Pojmenuj test podle chování a výsledku, například `MoveTask_ToEmptyColumn_ReindexesBothColumns`.
- Odděl přípravu dat, provedení akce a ověření výsledku (Arrange–Act–Assert).
- Ověřuj veřejný kontrakt nebo chování uživatele. Konkrétní text a pořadí kontroluj tam, kde jsou součástí tohoto kontraktu.
- Každý test má vlastní data a může běžet samostatně i v libovolném pořadí.
- Sdílené fixtures zajišťují prostředí a životní cyklus služeb; sdílený měnitelný stav nesmí ovlivňovat výsledky testů.
- Čas a externí služby ovládej testovacími závislostmi. Pro čekání na UI používej automaticky opakované assertions, nikoli pevné pauzy.
- Pro E2E preferuj přístupné role a názvy prvků, případně stabilní testovací identifikátory.
- Vedoucí testování doplní k reprodukovatelné chybě regresní test, pokud ji lze smysluplně automatizovat; po opravě autorem funkce ověří jeho průchod.
- Coverage používej k hledání mezer. Samotné procento ani počet testů neprokazují správnost aplikace.

Testovací soubory seskupuj podle funkční oblasti. Integrační backendové testy
odděl od jednotkových do samostatného projektu, aby měly vlastní závislosti
a daly se spouštět zvlášť. Obecnou prázdnou šablonu nekopíruj do každé oblasti;
opakovanou přípravu prostředí sdílej pomocí fixtures a jednoduchých pomocných funkcí.

## Pravidla pro pull request a CI

- Autor předává implementovanou funkci vedoucímu testování, který následně doplní odpovídající testy. Nejasná akceptační kritéria se vyjasní před stanovením očekávaných výsledků testů.
- U otevřeného PR funkce se doplnění testů a vyřešení nálezů provede před sloučením. U již sloučené funkce se navazující testovací PR propojí s původní změnou.
- Před sloučením musí projít relevantní testy, lint a sestavení. V ochraně větve mají být tyto kontroly povinné.
- CI ukládá výsledky testů a při selhání E2E také diagnostiku, například trace a screenshot.
- Nespolehlivý test se opraví; opakované spouštění nesmí trvale nahrazovat řešení příčiny.
- Ruční průzkumné testování doplňuje automatizaci. Opakovatelné chyby eviduj v issue s verzí, kroky a očekávaným a skutečným výsledkem.

Existující [workflow](../.github/workflows/ci.yml) sestavuje API, spouští tři
backendové projekty a kontroluje lint a build frontendu. Frontendové, integrační
a E2E testy ani publikování testovacích reportů zatím neobsahuje.
Nastavení ochrany větve nebylo ověřeno; samotný workflow sloučení neblokuje.

## Spuštění existujících testů

Z kořene repozitáře s nainstalovaným .NET 10 SDK:

```bash
dotnet test tests/StudentFlow.Api.Tests/StudentFlow.Api.Tests.csproj -c Release
dotnet test tests/StudentFlow.Core.Tests/StudentFlow.Core.Tests.csproj -c Release
dotnet test tests/StudentFlow.Data.Tests/StudentFlow.Data.Tests.csproj -c Release
```

Collector pro coverage je již přidaný. Pro sběr při běhu konkrétního projektu:

```bash
dotnet test tests/StudentFlow.Api.Tests/StudentFlow.Api.Tests.csproj -c Release --collect:"XPlat Code Coverage"
```

Příkazy pro Vitest a Playwright budou doplněny při jejich zavedení; v aktuálním
`package.json` testovací skripty nejsou.

## Nejbližší implementační kroky

1. Zavést Vitest a pokrýt existující `moveTask` a zpracování odpovědí API.
2. Zavést integrační testy skutečných HTTP endpointů pomocí WebApplicationFactory.
3. Zavést Playwright a základní scénář načtení Kanbanu proti spuštěné aplikaci.
4. Zapojit nové sady a jejich reporty do CI a nastavit povinné kontroly před sloučením.
5. Po předání autentizace, databáze a ukládání jejich autory doplní vedoucí testování testy oprávnění, izolace týmů a trvalého uložení změn. Prázdné backendové testy nahradí skutečnými testy po vzniku příslušné logiky.

## Podklady

- [Microsoft: pravidla jednotkového testování](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Microsoft: integrační testy ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Microsoft: strategie databázového testování](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
- [Playwright: doporučené postupy](https://playwright.dev/docs/best-practices)
- [Technologický návrh StudentFlow](ELABORATION/TECHNOLOGICAL_DESIGN.md)
