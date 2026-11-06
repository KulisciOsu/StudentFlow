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
| Jednotkové a komponentové frontendové | Validace a zpracování odpovědí API. | TypeScript, Vitest | 28 testů registrace, loginu, obnovy identity, logoutu a pravidel navigace. Komponentové testy a `moveTask` zatím chybí. |
| Integrační API | HTTP routování, stavové kódy, serializace, validace a autentizace. | xUnit, WebApplicationFactory, PostgreSQL 17 | 65 testů registrace, loginu, session, hashování, CSRF, limitů, vývojového seedu a chráněného integračního vzoru přes skutečnou pipeline. |
| Integrační databázové | Dotazy, zápisy, omezení a migrace na stejném databázovém systému jako produkce. | xUnit, Npgsql, skutečná PostgreSQL 17 | 30 testů v `StudentFlow.Data.IntegrationTests`: migrace, účty, normalizace, transakce, souběhy a session včetně rollbacku rehashe. Testcontainers zatím není zaveden. |
| E2E | Kritické uživatelské průchody přes prohlížeč, frontend a API. | TypeScript, Playwright | 28 scénářů v Chromium, Firefox a WebKit (84 testů): HTTPS, registrace obou typů, login, refresh, role, logout a replay cookie, více karet, výpadky, klávesnice, mobil a tablet s dotykovým ovládáním. |
| Frontendová integrace | Propojení dashboardu a formuláře projektu s routováním a stavem identity. | TypeScript, Playwright, řízené odpovědi API | 8 scénářů v `project-creation-integration.spec.ts`: anonymní přístup, čekání a výpadek ověření, omezení vyučujícího, filtry dashboardu, oba vstupy do založení projektu a průchod člena na mobilu, tabletu a desktopu. |

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

[Workflow](../.github/workflows/ci.yml) sestavuje API, spouští tři backendové
projekty a kontroluje Vitest, lint, audit závislostí a build frontendu. Audit
zahrnuje i vývojové závislosti a odmítá nálezy střední a vyšší závažnosti.
Samostatný E2E job spouští skutečné
API i Vite přes stejné HTTPS prostředí jako lokální vývoj. Ukládá TRX, JUnit
a HTML reporty a při selhání browser testu také screenshot a trace. Opakování
selhaných testů je vypnuté, aby nespolehlivý první běh nezakrylo opakování.
Samostatný databázový job používá PostgreSQL 17, spouští databázové i HTTP
integrační testy a ukládá TRX výsledky. E2E job má vlastní službu PostgreSQL 17.
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

Frontendovou jednotkovou sadu spusťte pomocí `npm --prefix src/studentflow.client test`.
Vitest vyhledává pouze `src/**/*.test.ts`; Playwright scénáře spouští samostatný příkaz.

Audit všech frontendových závislostí (včetně nástrojů pro vývoj a testování):

```bash
npm --prefix src/studentflow.client run audit:dependencies
```

Příkaz potřebuje dostupný npm registr. Návratový kód 0 znamená, že audit
nenašel nic nad nastaveným prahem; chybu přístupu k registru nelze vydávat
za čistý audit. Použití dočasného override a jeho odstranění popisuje
[DEVELOPMENT.md](DEVELOPMENT.md).

## Spuštění E2E proti API

Nejdříve připravte [vývojové prostředí](DEVELOPMENT.md), zejména Node.js 22,
.NET 10 a vývojový HTTPS certifikát. Z kořene repozitáře:

```bash
npm --prefix src/studentflow.client ci
node scripts/dev.mjs setup
```

Potom ze složky `src/studentflow.client`:

```bash
npx playwright install --with-deps
export STUDENTFLOW_E2E_DATABASE_URL='postgresql://tester:your-local-test-password@127.0.0.1:55417/postgres'
npm run test:e2e
```

Playwright sám spustí API na `https://localhost:7082` a Vite na
`https://localhost:60357`, vyčká na oba a po testech je zastaví. Existující
servery nepřebírá: před spuštěním uvolněte jejich porty. Ověření certifikátu
se vynechává pouze pro vlastní vývojové certifikáty v testovacím prostředí.

Je nutný **vyhrazený testovací server PostgreSQL 17** a účet s `CREATEDB`.
Skript `scripts/e2e-api.mjs` vytvoří databázi `studentflow_e2e_<UUID>`, aplikuje
migrace a předá API její connection string. Po ukončení databázi odstraní.
Bez explicitního `STUDENTFLOW_E2E_DATABASE_URL` testy selžou; nepřebírají aplikační
databázi z `.env`. V PowerShellu nastavte proměnnou pomocí `$env:STUDENTFLOW_E2E_DATABASE_URL`.

Testovací API má limity registrace a loginu zvýšené na 200 požadavků na IP,
aby se paralelní scénáře nesdílely o běžný uživatelský limit. Skutečné chování
`429` a `Retry-After` ověřují HTTP testy s limitem 1. Produkční výchozí limity
se tím nemění.

Volba `-- --project=chromium` spustí jediný prohlížeč. Reporty jsou v ignorovaných
`playwright-report/` a `test-results/`. Úspěšné registrace a duplicita používají
skutečný backend i databázi; pouze scénář výpadku sítě záměrně přeruší požadavek.
Přesnou časovou hranici 8 hodin a 30 dní ověřují HTTP testy řízeným
`TimeProvider`; browser testy ověřují cookie, refresh a serverové zneplatnění.

Sada `project-creation-integration.spec.ts` používá řízené odpovědi autentizace,
aby odděleně ověřila propojení dashboardu KAN-81 a formuláře KAN-86 s přihlášením KAN-46. Běží stejným
příkazem společně s E2E. Neověřuje skutečné session ani uložení projektu;
formulář projektu zatím není napojený na zapisující API.

Tabletové scénáře v `tests/tablet.spec.ts` zapínají `hasTouch` a ovládají prvky
pomocí `tap()`. Pokrývají viewporty 744×1133, 768×1024, 820×1180, 1024×768
a 1180×820, dvousloupcovou registraci, chybový souhrn, volbu typu, viditelnost
hesla a trvalost přihlášení. Oba typy účtu projdou skutečnou registrací,
loginem, otočením a logoutem proti API a PostgreSQL. Zmenšení výšky na 480 px
simuluje omezený prostor při otevřené klávesnici. Report přikládá náhledy obou
formulářů a přihlášeného přehledu. Jde o emulaci viewportu a dotyku; skutečnou
softwarovou klávesnici a chování na fyzickém tabletu tato emulace neověřuje.
Pro aktuální fázi KAN-112 uživatel 4. 11. 2026 přijal testy přes PC a emulaci;
požadavek na fyzický telefon/tablet se skutečnou klávesnicí prozatím zrušil.
Neověřené chování fyzických zařízení zůstává známým omezením.

## Spuštění databázových integračních testů

Použijte **vyhrazený testovací server PostgreSQL 17**. Každý test vytvoří vlastní
databázi `studentflow_test_<UUID>` a po sobě ji odstraní. Účet musí mít oprávnění
`CREATEDB`. Testy nikdy nepřebírají aplikační `ConnectionStrings__DefaultConnection`
a při chybějící testovací konfiguraci selžou s vysvětlením; nepřeskočí se.

V Bash nastavte connection string pro tento vyhrazený server:

```bash
export STUDENTFLOW_TEST_DATABASE='Host=127.0.0.1;Port=55417;Database=postgres;Username=tester;Password=your-local-test-password;SSL Mode=Disable'
dotnet test tests/StudentFlow.Data.IntegrationTests/StudentFlow.Data.IntegrationTests.csproj -c Release
dotnet test tests/StudentFlow.Api.IntegrationTests/StudentFlow.Api.IntegrationTests.csproj -c Release
```

V PowerShellu nastavte stejný connection string pomocí
`$env:STUDENTFLOW_TEST_DATABASE = '...'`. CI vytváří vlastní službu `postgres:17`.

Sada pokrývá nový i existující stav databáze, zachování účtů při upgradu,
odmítnutí kolizí bez částečné migrace, právě jeden typ účtu a jeho souběžné změny
ve třech úrovních izolace, Unicode normalizaci, souběžnou duplicitu registrace,
rollback zápisu při chybě role, podmíněný rehash a integritu tabulky session.
HTTP sada přidává oba typy registrace, nezměněné heslo včetně mezer, náhodnou sůl
a pracovní faktor hashe, Unicode hranice, chybný JSON, CSRF, duplicitu, stavové
kódy 400/403/409/413/415/429/503 a bezpečný tvar odpovědí. Session po registraci
nevzniká. Společná infrastruktura je v `StudentFlow.Testing`; každá sada má vlastní
databáze. Dalších 26 HTTP případů pokrývá shodné odmítnutí neexistujícího,
neaktivního a nesprávného účtu, rehash, vlastnosti cookie, chybějící/padělaný
token, přesnou expiraci bez prodlužování, rotaci stejného prohlížeče, zachování
jiného zařízení, CSRF v hlavičce vázaný na identitu, opakovaný logout, replay a bezpečné
selhání při chybě ukládání/čtení/zneplatnění session. Datové testy doplňují
změnu hesla mezi ověřením a zápisem, deaktivaci a rollback rehashe při chybě vložení.

`HandoffExampleTests` přidává tři HTTP případy pro vzor z KAN-115. Ověřuje
anonymní `401`, použití vlastníka session při podstrčeném `userId`, oba typy
účtu, učitelské omezení `403` bez ztráty přihlášení a odmítnutí po logoutu.
Controller se kompiluje z `examples/authentication/` pouze do testovacího
projektu a načítá se jeho volitelným `AddApplicationPart`; do produkčního
API se automaticky nezapojuje. Postup použití popisuje
[integrační návod](ELABORATION/auth/INTEGRATION_GUIDE.md).

`AuthRequestBodyLimitTests` obsahuje čtyři případy přesné hranice JSON 16 KiB
pro registraci a login bez `Content-Length`. Ověřuje přijetí 16 384 bajtů
a odmítnutí 16 385 bajtů bez zápisu. Test nedostupné databáze v
`RegistrationTests` zachytává také výstup loggerů a odmítá únik hesla nebo
citlivého obsahu vnitřní výjimky.

`DevelopmentAccountSeedTests` ověřuje vytvoření účtů člena i vyučujícího
při vývojovém startu včetně přihlášení a odhlášení, opakované i souběžné spuštění
bez duplicit a zachování existujícího hesla, typu, jména a aktivity. Ověřuje
také doplnění chybějícího účtu při existenci druhého. Seed se nesmí provést v prostředí
`Production` ani `Testing`, i kdyby byl v konfiguraci povolený; ve vývoji lze
seeding vypnout. E2E spouštěč jej vypíná, aby si automatické testy zakládaly
pouze vlastní účty.

## Nejbližší implementační kroky

1. Rozšířit Vitest o existující `moveTask` a komponenty po předání příslušné funkce.
2. Ověřit další projektová API a jejich oprávnění po předání implementace.
3. Doplnit průzkumné scénáře nad projektovým dashboardem po předání jeho implementace.
4. Nastavit povinné kontroly před sloučením; ochrana větve zůstává neověřená.
5. Po předání autentizace, databáze a ukládání jejich autory doplní vedoucí testování testy oprávnění, izolace týmů a trvalého uložení změn. Prázdné backendové testy nahradí skutečnými testy po vzniku příslušné logiky.

## Podklady

- [Microsoft: pravidla jednotkového testování](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
- [Microsoft: integrační testy ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Microsoft: strategie databázového testování](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy)
- [Playwright: doporučené postupy](https://playwright.dev/docs/best-practices)
- [Playwright: emulace zařízení a viewportu](https://playwright.dev/docs/emulation)
- [Technologický návrh StudentFlow](ELABORATION/TECHNOLOGICAL_DESIGN.md)
