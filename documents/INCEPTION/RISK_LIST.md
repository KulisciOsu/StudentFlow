## 4.4 Risk-list – aktuální stav

**Aktualizováno:** 20. 11. 2026.
**Původní verze:** [archiv risk listu před touto aktualizací](ARCHIVE/RISK_LIST_2026-11-20.md).

Tým sleduje devět rizik. **R8 je mitigováno pro současný prototyp, sedm rizik částečně a R4 zatím mitigováno není.**

Původní opatření a hodnocení pravděpodobnosti, dopadu a závažnosti jsou zachované. Sloupce **Mitigováno** a **Zdůvodnění** popisují aktuální stav.

### Význam hodnocení

| Mitigováno | Význam |
| --- | --- |
| **Ano** | Opatření je zavedené a ověřené v uvedeném rozsahu. |
| **Částečně** | Část opatření funguje, ale zbývají další kroky. |
| **Ne** | Potřebná opatření zatím nejsou zavedená. |
| **Neověřeno** | Chybí podklady pro spolehlivé hodnocení. |

### Přehled rizik

| # | Riziko | Pravděpodobnost | Dopad | Závažnost | Opatření | Mitigováno | Zdůvodnění |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R1 | Projekt se nestihne dokončit v termínu (nedostatek času). | střední | vysoký | vysoká | Práce rozdělená do sprintů v Jiře s milníky a průběžnou kontrolou postupu. Nejdřív jádro systému (role, úkoly, Kanban), až potom statistiky. Časová rezerva před odevzdáním. | **Částečně** | Základ aplikace už funguje. Zbývá dokončit další funkce a závěrečné testování, takže riziko nedodržení termínu trvá. |
| R2 | Ztráta znalostí o systému po skončení semestru: v příštím semestru zůstane v týmu jen jeden člen. | střední | vysoký | vysoká | Průběžná dokumentace kódu, architektury a API. README s postupem spuštění. Předávací schůzka a zapojení zůstávajícího člena do všech částí systému. | **Částečně** | Existují návody ke spuštění a popisy částí systému. Samostatné převzetí aplikace zůstávajícím členem zatím není doložené. |
| R3 | Chybné nastavení oprávnění: uživatel uvidí nebo změní data, ke kterým nemá mít přístup (např. projekt jiného týmu). | střední | vysoký | vysoká | Předem navržená matice rolí a oprávnění. Kontrola oprávnění na backendu u každého požadavku, ne jen skrytí prvků ve frontendu. Testy pro každou roli. | **Částečně** | Server kontroluje role a přístup k projektům a tyto kontroly mají testy. U přesunů úkolů zbývá doplnit ochranu požadavků a sjednotit odpovědi při přístupu k cizím projektům. |
| R4 | Problémy s ukládáním souborů a příloh: zaplnění úložiště nebo databáze, příliš velké či zavirované soubory. | nízká | vysoký | vysoká | Maximální velikost souboru a limit celkového objemu příloh. Omezení povolených typů souborů. Kontrola na frontendu i backendu. Zvážit ukládání souborů mimo databázi (v databázi jen odkaz). | **Ne** | Nahrávání a stahování příloh zatím není implementované. Limity, kontroly souborů a ochranu úložiště je teprve potřeba zavést. |
| R5 | Chyby při přenosu dat mezi frontendem, aplikačním serverem a databází (např. přesun úkolu se neuloží a uživatel o tom neví). | střední | vysoký | vysoká | Jednotný error handling na aplikačním serveru, který chybu zachytí dřív, než se dostane do frontendu. Server vždy vrátí výsledek operace (úspěch nebo srozumitelnou chybu). Validace dat před zápisem do databáze. | **Částečně** | Při chybě přesunu se karta vrátí a uživatel dostane hlášení. Zbývá ověřit souběžné změny a chybové stavy dalších operací. |
| R6 | Nedostatečná spolupráce členů týmu: jednotlivé části kódu na sebe nenavazují. | nízká | vysoký | vysoká | Pravidelné schůzky na Discordu. Úkoly evidované v Jiře, společný kód na GitHubu. Průběžná komunikace napříč Jirou, GitHubem a Discordem. | **Částečně** | Části aplikace od více členů týmu jsou propojené a fungují společně. U dalších funkcí je potřeba pokračovat v domlouvání rozhraní a průběžné integraci. |
| R7 | Nedostatečné testování kvůli malým zkušenostem týmu s testováním. | vysoká | střední | vysoká | Testovací plán s klíčovými scénáři (přihlášení, role, úkoly, soubory). Testování průběžně po každé funkci, ne až na konci. Nastudování základů testování. | **Částečně** | Automatické testy jsou zavedené a procházejí. Zbývá ověřit celý rozsah projektu a dokončit závěrečné testování. |
| R8 | Obtížná implementace Drag & Drop na Kanban nástěnce. | střední | střední | střední | Včasný prototyp a použití ověřené knihovny. Záložní řešení: změna stavu úkolu výběrem ze seznamu, bez přetahování. | **Ano** | Přetahování funguje a přesuny se ukládají. Přetažení a návrat při chybě jsou otestované v Chromium. Mobilní varianta nabízí výběr cílového sloupce. Riziko je ošetřené pro současný prototyp. |
| R9 | Chybné nebo nekvalitní výstupy členů sekcí. | střední | střední | střední | Jasné zadání úkolů. Kontrola výstupů vedoucím sekce před začleněním do projektu a včasné vrácení k opravě. | **Částečně** | Změny vyžadují dvě schválení a procházejí automatickými kontrolami. Zbývá vynutit úspěšné testy před sloučením a aktualizovat dokumentaci. |

Hodnocení vychází z implementace, testů a [úspěšného běhu automatických kontrol](https://github.com/KulisciOsu/StudentFlow/actions/runs/44636006926). Při další revizi se aktualizuje stav a jeho zdůvodnění.
