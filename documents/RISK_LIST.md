## 4.4 Risk-list

Tým eviduje devět rizik, sedm z nich má vysokou závažnost. Nejvíce projekt ohrožuje nedostatek času, odchod členů týmu po semestru a rizika spojená s bezpečností a integritou dat.

| # | Riziko | Pravděpodobnost | Dopad | Závažnost | Opatření | Odpovědnost |
| --- | --- | --- | --- | --- | --- | --- |
| R1 | Projekt se nestihne dokončit v termínu (nedostatek času). | střední | vysoký | vysoká | Práce rozdělená do sprintů v Jiře s milníky a průběžnou kontrolou postupu. Nejdřív jádro systému (role, úkoly, Kanban), až potom statistiky. Časová rezerva před odevzdáním. | Vedoucí týmu |
| R2 | Ztráta znalostí o systému po skončení semestru: v příštím semestru zůstane v týmu jen jeden člen. | vysoká | vysoký | vysoká | Průběžná dokumentace kódu, architektury a API. README s postupem spuštění. Předávací schůzka a zapojení zůstávajícího člena do všech částí systému. | Vedoucí týmu |
| R3 | Chybné nastavení oprávnění: uživatel uvidí nebo změní data, ke kterým nemá mít přístup (např. projekt jiného týmu). | střední | vysoký | vysoká | Předem navržená matice rolí a oprávnění. Kontrola oprávnění na backendu u každého požadavku, ne jen skrytí prvků ve frontendu. Testy pro každou roli. | Vedoucí backendu |
| R4 | Problémy s ukládáním souborů a příloh: zaplnění úložiště nebo databáze, příliš velké či zavirované soubory. | střední | vysoký | vysoká | Maximální velikost souboru a limit celkového objemu příloh. Omezení povolených typů souborů. Kontrola na frontendu i backendu. Zvážit ukládání souborů mimo databázi (v databázi jen odkaz). | Vedoucí frontendu ve spolupráci s vedoucím backendu |
| R5 | Chyby při přenosu dat mezi frontendem, aplikačním serverem a databází (např. přesun úkolu se neuloží a uživatel o tom neví). | střední | vysoký | vysoká | Jednotný error handling na aplikačním serveru, který chybu zachytí dřív, než se dostane do frontendu. Server vždy vrátí výsledek operace (úspěch nebo srozumitelnou chybu). Validace dat před zápisem do databáze. | Vedoucí backendu |
| R6 | Nedostatečná spolupráce členů týmu: jednotlivé části kódu na sebe nenavazují. | střední | vysoký | vysoká | Pravidelné schůzky na Discordu. Úkoly evidované v Jiře, společný kód na GitHubu. Průběžná komunikace napříč Jirou, GitHubem a Discordem. | Vedoucí týmu |
| R7 | Nedostatečné testování kvůli malým zkušenostem týmu s testováním. | vysoká | střední | vysoká | Testovací plán s klíčovými scénáři (přihlášení, role, úkoly, soubory). Testování průběžně po každé funkci, ne až na konci. Nastudování základů testování. | Vedoucí testování |
| R8 | Obtížná implementace Drag & Drop na Kanban nástěnce. | střední | střední | střední | Včasný prototyp a použití ověřené knihovny. Záložní řešení: změna stavu úkolu výběrem ze seznamu, bez přetahování. | Vedoucí frontendu |
| R9 | Chybné nebo nekvalitní výstupy členů sekcí. | střední | střední | střední | Jasné zadání úkolů. Kontrola výstupů vedoucím sekce před začleněním do projektu a včasné vrácení k opravě. | Vedoucí příslušné sekce (frontend, backend, testování) |

Závažnost vychází z kombinace pravděpodobnosti a dopadu: vysoký dopad při střední nebo vysoké pravděpodobnosti a vysoká pravděpodobnost při středním dopadu znamenají vysokou závažnost.

