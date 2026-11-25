# 6.5 Prototyp: 20 % implementace

> **Krok 8 řetězce: prototyp.** Navazuje na [případy užití](../INCEPTION/USE_CASE.md), [wireframy](Wireframe.pdf) a [návrh architektury](ARCHITECTURAL_DESIGN.md). Prototyp ověřuje, že architektura odpovídá požadavkům (zadání kap. 6: „ověříme architekturu“).

Prototyp je první funkční řez celou architekturou: frontend → REST API → doménová pravidla → datová vrstva → PostgreSQL. Podle zadání předmětu má tým do konce prosince 2026 dokončit 20 % technické implementace. Jde o jádro architektury a funkce, které ověřují největší technická rizika, aby zbylých 80 % v letním semestru stavělo na ověřeném základu.

Výchozí stav odpovídá větvi `main` k 22. 11. 2026; opravy A02–A04 jsou zohledněny v [PR #24](https://github.com/KulisciOsu/StudentFlow/pull/24) a po jeho sloučení platí také pro `main`.

## Co prototyp ověřuje

Zadání (kap. 6.5) uvádí, co má prototyp ověřit. Odpovědi:

| Otázka ze zadání | Výsledek | Doklad |
| --- | --- | --- |
| Funguje zvolená technologie? | Ano. React s Vite, ASP.NET Core na .NET 10, PostgreSQL 17 a Dapper fungují společně ve vývojovém prostředí i v CI. | [Vývojové prostředí](../DEVELOPMENT.md), běhy CI v GitHub Actions |
| Lze propojit komponenty? | Ano. Pět funkčních částí od pěti členů týmu prochází všemi vrstvami a funguje v jedné aplikaci. | Tabulka níže, [rozdělení práce](ROZDELENI_PRACE.md) |
| Funguje komunikace s databází? | Ano. Schéma vzniká SQL migracemi, zápisy probíhají v transakcích, integrační testy běží nad skutečnou PostgreSQL 17. | `StudentFlow.Data.IntegrationTests` |
| Lze využít externí API? | Netýká se. Napojení na externí nástroje je mimo rozsah projektu. | [Rozsah projektu](../INCEPTION/SCOPE_OF_PROJECT.md) |
| Lze implementovat klíčovou funkcionalitu? | Ano. Kanban nástěnka s přetahováním a ukládáním, přihlášení a role, založení projektu a správa členů. | E2E testy v `src/studentflow.client/tests/` |

## Rozsah prototypu podle případů užití

| UC | Případ užití | Epik v Jiře | Implementoval | Stav | Poznámka |
| --- | --- | --- | --- | --- | --- |
| UC12 | Zaregistrovat se a přihlásit | KAN-46 (KAN-100 až KAN-117, KAN-131 až KAN-133) | Kryštof Kristuf | **Hotovo** | Registrace s pravidly hesla a souhlasem se zásadami, přihlášení, „Zůstat přihlášen“, odhlášení, rozcestí podle typu účtu. |
| UC04 | Založit týmový projekt | KAN-48 (KAN-86 až KAN-89) | Vladimír Pospíšil | **Hotovo** | Projekt, tým, vedoucí a výchozí sloupce vznikají v jedné transakci. |
| UC13 | Spravovat členy týmu | KAN-50 (KAN-90 až KAN-94) | Adam Gebauer | **Hotovo** | Seznam členů, přidání podle e-mailu, odebrání s potvrzením. |
| UC06 | Zobrazit projekt a jeho průběh | KAN-47 (KAN-80 až KAN-84) a nástěnka (KAN-96 až KAN-99) | Honza Sebroň, Marek Szczepanik | **Částečně** | Přehled projektů a úkolů člena a nástěnka projektu fungují. Přehled vyučujícího zatím projekty nevypisuje. |
| UC01 | Aktualizovat stav úkolu | Nástěnka (KAN-96 až KAN-99, KAN-122) | Marek Szczepanik | **Částečně** | Přesun přetažením (na mobilu výběrem sloupce), uložení, oprávnění a vrácení karty při chybě. Historie změn a detekce souběžných změn jsou rozšířením na letní semestr. |
| UC02, UC03, UC05, UC07 až UC11, UC14 | ostatní | | | **Letní semestr** | Viz [plán projektu](PLAN_PROJEKTU.md). Úkoly na nástěnce jsou zatím ukázková data. |

## Pokrytí požadavků

| Požadavek | Stav | Čím je pokryt |
| --- | --- | --- |
| FP01 Registrace a přihlášení | Hotovo | UC12 |
| FP02 Uživatelské role | Částečně | Typ účtu a role v projektu fungují. Vyučující má zatím přístup ke všem nástěnkám, viz omezení A01. |
| FP03 Správa projektů | Částečně | Založení projektu (UC04). Úprava údajů (UC14) chybí. |
| FP04 Správa týmů | Hotovo | UC04, UC13 |
| FP05 až FP07 Úkoly, přiřazení, termíny | Letní semestr | UC05 |
| FP08 Kanban nástěnka | Hotovo | UC01 v základním rozsahu |
| FP09, FP10 Komentáře, přílohy | Letní semestr | UC02, UC03, UC08 |
| FP11 Přehled práce | Hotovo | Přehled projektů a úkolů přihlášeného člena |
| FP12 Statistiky | Částečně | Souhrny na přehledu člena. Statistiky jednotlivých členů (UC07) chybí. |
| FP13 až FP15 Vyučující, milníky, oznámení | Letní semestr | UC09, UC10, UC11 |
| NP01, NP02 Rychlost a plynulost | Ověřeno | E2E testy s limity 2 s a 0,5 s |
| NP03 Bezpečnost | Částečně | Session, CSRF (včetně založení projektu), hashování a kontrola oprávnění fungují. Zbývá omezení A01. |
| NP04 Integrita dat | Ověřeno | Transakce a omezení v databázi, integrační testy |
| NP05 Práce se soubory | Letní semestr | Spolu s UC03 |
| NP06, NP10 Chyby a spolehlivost | Částečně | Nástěnka i autentizace jsou ověřené. Založení projektu vrací srozumitelné chyby 400 (validace), 403 (CSRF/oprávnění) bez neúplného zápisu; A03 vyřešeno v PR #24. |
| NP07 Kompatibilita | Ověřeno | E2E testy v Chromium, Firefox a WebKit |
| NP09 Responzivita | Ověřeno | E2E testy pro mobil a tablet |
| NP11 Ochrana osobních údajů | Hotovo | Souhlas se zásadami při registraci |

## Ověření architektury

Fáze Elaboration má ověřit, že navržená architektura odpovídá problému. Architektura z [návrhu architektury](ARCHITECTURAL_DESIGN.md) je webová aplikace klient–server: React frontend, REST API v ASP.NET Core rozdělené na `StudentFlow.Api`, `StudentFlow.Core` a `StudentFlow.Data` a databáze PostgreSQL. Tabulka ukazuje, který požadavek nebo riziko vedlo ke kterému rozhodnutí a čím se to ověřuje.

| Požadavek / riziko | Architektonické rozhodnutí | Ověření |
| --- | --- | --- |
| NP01 Rychlost odezvy | Nástěnka se načítá několika jednoduchými dotazy REST API nad relační databází PostgreSQL. | E2E test „člen načte nástěnku do 2 s“ (`board.spec.ts`). |
| NP02 Plynulost ovládání | Frontend zobrazí přesun hned a změnu uloží na pozadí jedním požadavkem. | E2E test „přesunutý úkol se projeví do 0,5 s, uloží do 2 s“ (`board.spec.ts`). |
| NP03 Bezpečnost, R3 Chybná oprávnění | Serverová session v `HttpOnly` cookie, CSRF token u zápisů, hesla hashovaná PBKDF2. Oprávnění kontroluje API při každém požadavku podle pravidel v Core, ne jen frontend. | Integrační testy autentizace a session, testy pravidel oprávnění v `StudentFlow.Core.Tests`. Zůstává omezení A01; A02 je opraveno v PR #24 a ověřeno integračními testy. |
| NP04 Integrita dat | Relační model s cizími klíči, unikátními indexy a kontrolami v databázi. Založení projektu i změny členství probíhají v jedné transakci. | Integrační testy nad skutečnou PostgreSQL 17 (`StudentFlow.Data.IntegrationTests`). |
| NP05 Práce se soubory, R4 Soubory a přílohy | **Otevřené rozhodnutí:** soubory ukládat mimo databázi a v databázi držet jen odkaz, limit 20 MB kontrolovat na frontendu i v API. | Ověří se v letním semestru (UC03, UC08). |
| NP06 Zpracování chyb, NP10 Spolehlivost, R5 Chyby při přenosu dat | API vrací chyby v jednotném tvaru s kódem chyby. Frontend chybu nikdy nevydává za úspěch a při neúspěšném přesunu vrátí kartu na původní místo. | E2E test s odpovědí `500` při přesunu (`board.spec.ts`) a API integrační testy založení projektu (`ProjectCreationApiTests.cs`) pro CSRF, roli, povinný termín a chybové stavy bez zápisu. |
| NP07 Kompatibilita | Standardní webové technologie bez doplňků prohlížeče. | E2E testy v Chromium, Firefox a WebKit. |
| NP08 Použitelnost | Rozhraní navržené podle wireframů, jednotné formuláře a česká terminologie. | Průchod podle wireframů a scénáře v [obrazovkách a stavech](auth/SCREENS_AND_FLOWS.md). |
| NP09 Responzivita | Responzivní rozvržení pro počítač, tablet a mobil. Na mobilu se úkol přesouvá výběrem cílového sloupce místo přetahování. | E2E testy pro mobil a tablet (`tablet.spec.ts`, `board.spec.ts`). |
| NP11 Ochrana osobních údajů | Účet ukládá jen jméno, e-mail a typ účtu. Registrace ukládá verzi a čas potvrzení zásad. | Testy registrace (`registration.spec.ts`, `RegistrationTests.cs`). |
| R2 Ztráta znalostí po semestru | Oddělené vrstvy s jasnou odpovědností, schéma v čitelných SQL migracích, návod ke spuštění. | Člen, který zůstane, spustí aplikaci podle [DEVELOPMENT.md](../DEVELOPMENT.md) (cíl C10). |
| R6 Nedostatečná spolupráce | Rozhraní mezi částmi je dané REST API a kontrakty (API kontrakt, OpenAPI), takže každý člen mohl stavět svou funkční část od frontendu po databázi. | Pět epiků od pěti členů funguje v jedné aplikaci (viz [rozdělení práce](ROZDELENI_PRACE.md)). |
| R7 Nedostatečné testování | Testy po vrstvách: xUnit pro Core, API a Data, Vitest pro frontend, Playwright pro celé průchody. CI je spouští u každého pull requestu. | Výsledky CI v GitHub Actions. |
| R8 Drag & Drop | Ověřená knihovna `@dnd-kit`, záložní výběr sloupce na mobilu. | E2E testy přesunu, riziko vedené jako mitigované. |

**Závěr ověření:** Architektura pokrývá všechny nefunkční požadavky kromě NP05, pro který zbývá rozhodnout způsob ukládání souborů. Funkce plánované na letní semestr (úkoly, soubory, komentáře, historie, oznámení, milníky) přidávají nové tabulky a endpointy do stávajících vrstev a samotnou architekturu nemění. Prototyp ji ověřil pěti funkčními částmi, které na sebe navazují (viz výše).

## Jak prototyp snížil rizika

Podrobné hodnocení je v [risk-listu](../INCEPTION/RISK_LIST.md). Prototyp plně ošetřil riziko R8 (Drag & Drop) a částečně R2, R3, R5, R6 a R7. Riziko R4 (soubory a přílohy) zatím ošetřené není, protože přílohy jsou naplánované na letní semestr.

## Známá omezení prototypu

Omezení vycházejí z auditu souladu implementace s dokumentací z 20. 11. 2026 (A01 až A10). Tam, kde to šlo, tým upravil dokumentaci. Zbývající body jsou zapsané jako omezení s navrženým řešením.

| # | Omezení | Dopad | Navržené řešení |
| --- | --- | --- | --- |
| A01 | Účet typu Vyučující může číst nástěnky všech projektů (jen pro čtení). | Přidělování přístupu vedoucím (UC09) zatím neexistuje. Protože si účet vyučujícího může založit kdokoli, nesmí aplikace do té doby pracovat se skutečnými daty. | Implementovat UC09 a omezit čtení nástěnky na přidělené projekty. Do té doby jde o vědomé zjednodušení prototypu nad ukázkovými daty. |
| A02 | Původně chyběla CSRF kontrola založení projektu; přesun úkolu chrání KAN-144. | **Vyřešeno v PR #24 (KAN-143).** Bez tokenu API vrací `403 CSRF_INVALID`, nic neuloží. | `CsrfValidationFilter` na `POST /api/projects`, integrační testy a původní klientské `csrfRequest`. |
| A03 | Původně pokus vyučujícího o založení projektu končil `500`. | **Vyřešeno v PR #24 (KAN-145).** Vyučující dostane `403 PROJECT_CREATION_FORBIDDEN`, bez zápisu. | Typ účtu se ověřuje před validací vstupu i před transakcí; pokryto unit a integračními testy. |
| A04 | Původně se lišily limity formuláře a API i povinnost termínu. | **Vyřešeno v PR #24 (KAN-146).** Název projektu do 200 znaků, tým do 120, popis do 1 000; termín povinný a ne v minulosti. | Validace `DueDate` na serveru, `required` a `min` na formuláři; dnešní datum v časové zóně `Europe/Prague`, integrační a E2E testy. |
| A05 | Přesun úkolu neukládá historii a neupozorní na souběžnou změnu. | Pozdější uložení může přepsat souběžný přesun. | Vyřešeno v dokumentaci: historie a detekce konfliktu jsou rozšířením UC01 na letní semestr. |
| A06 | Ruční instalační postup nepokrýval všechny migrace. | Po ručním postupu mohly chybět tabulky. | Řeší větev KAN-140 (Marek Szczepanik). |
| A07 | ER model připouštěl účet, který je současně členem i vyučujícím. | Rozpor s pravidlem jednoho typu účtu. | Vyřešeno: ER model upraven na jeden typ účtu. |
| A08, A09 | Návody popisovaly některé hotové funkce jako nehotové a přehled testů byl zastaralý. | Matoucí pro dalšího vývojáře. | Řeší větev KAN-140 (Marek Szczepanik). |
| A10 | Solution odkazoval na frontend s chybnou velikostí písmen v cestě. | Na Linuxu se frontend ze solution nenačte. | Řeší větev KAN-140 (Marek Szczepanik). |

## Spuštění a testy

Prototyp se spouští podle [vývojového prostředí](../DEVELOPMENT.md). Testy a jejich spuštění popisuje [testovací dokumentace](../TESTING.md).
