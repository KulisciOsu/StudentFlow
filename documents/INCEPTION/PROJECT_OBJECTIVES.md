## 4.6 Cíle projektu

> **Součást kroku 2 řetězce: vize.** Převádí [vizi projektu](VIZE_PROJEKTU.md) na měřitelné cíle. Funkční cíle odkazují na [požadavky](REQUIREMENTS.md) a [případy užití](USE_CASE.md).

Hlavním cílem je funkční webový systém, ve kterém tým rozdělí a sleduje práci a vyučující vidí podíl každého člena. Deset dílčích cílů ho rozkládá na ověřitelné kroky, každý s jasným kritériem splnění.

### Hlavní cíl

Do termínu odevzdání projektu navrhnout a vytvořit webový informační systém StudentFlow, ve kterém vedoucí týmu založí tým a projekt a rozdělí úkoly s termínem a prioritou, členové týmu mění stav svých úkolů na Kanban nástěnce a vyučující vidí průběh projektu a počet dokončených úkolů každého člena.

### Dílčí cíle: funkce systému

| # | Dílčí cíl | Kritérium splnění | Požadavky | Use case |
| --- | --- | --- | --- | --- |
| C1 | Umožnit uživateli registraci, přihlášení a odhlášení a rozlišit tři role. | Každá ze tří rolí vidí jen projekty, ke kterým má přístup, a testy oprávnění pro všechny role projdou. | FP01, FP02, NP03, NP11 | UC12 |
| C2 | Umožnit vedoucímu týmu vytvořit projekt a tým a spravovat jeho členy. | Tým lze založit, přidat do něj a odebrat z něj členy přímo v aplikaci, bez zásahu do databáze. | FP03, FP04 | UC04, UC13, UC14 |
| C3 | Umožnit vytvořit, upravit, smazat a přiřadit úkol s termínem a prioritou. | Úkol se všemi údaji se uloží a po obnovení stránky se zobrazí beze změny. | FP05, FP06, FP07 | UC05 |
| C4 | Zobrazit úkoly na Kanban nástěnce a umožnit jejich přesun mezi stavy. | Změna stavu se uloží do databáze. Při chybě se uživateli zobrazí hláška a úkol se vrátí do původního stavu. | FP08, NP01, NP02, NP10 | UC01, UC06 |
| C5 | Umožnit přidávat k úkolům komentáře a soubory. | Soubor do 20 MB se nahraje a jde stáhnout. Větší soubor nebo nepovolený typ systém odmítne. | FP09, FP10, NP05 | UC02, UC03, UC08 |
| C6 | Zobrazit statistiky práce jednotlivých členů týmu. | U každého člena je vidět počet přiřazených, dokončených a zpožděných úkolů a čísla odpovídají stavu na nástěnce. | FP11, FP12 | UC07 |
| C7 | Poskytnout vyučujícímu přehled o jeho týmech. | Vyučující vidí všechny projekty, ke kterým mu byl udělen přístup, a stav jejich úkolů na jedné obrazovce, aniž by byl členem týmu. | FP13 | UC06, UC09 |
| C8 | Umožnit plánování milníků a zobrazovat oznámení v aplikaci. | Milník s termínem a souvisejícími úkoly je vidět v přehledu projektu. Změna stavu úkolu nebo nový komentář vytvoří oznámení dotčeným členům. | FP14, FP15 | UC10, UC11 |

### Dílčí cíle: řízení projektu

| # | Dílčí cíl | Kritérium splnění |
| --- | --- | --- |
| C9 | Otestovat všechny funkce z rozsahu In Scope. | Ke každé oblasti In Scope existuje testovací scénář a všechny scénáře projdou před odevzdáním. |
| C10 | Zdokumentovat systém tak, aby ho šlo předat. | Dokumentace všech fází je odevzdaná v termínu a člen, který v týmu zůstane, dokáže systém podle README samostatně spustit. |

Stav plnění cílů k prvnímu milníku (20 % implementace) uvádí [prototyp](../ELABORATION/PROTOTYP.md).
