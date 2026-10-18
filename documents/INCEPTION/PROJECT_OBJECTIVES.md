## 4.6 Cíle projektu

Hlavním cílem je funkční webový systém, ve kterém tým rozdělí a sleduje práci a vyučující vidí podíl každého člena. Devět dílčích cílů ho rozkládá na ověřitelné kroky, každý s jasným kritériem splnění.

### Hlavní cíl

Do termínu odevzdání projektu navrhnout a vytvořit webový informační systém StudentFlow, ve kterém vedoucí týmu založí tým a projekt a rozdělí úkoly s termínem a prioritou, členové týmu mění stav svých úkolů na Kanban nástěnce a vyučující vidí průběh projektu a počet dokončených úkolů každého člena.

### Dílčí cíle: funkce systému

| # | Dílčí cíl | Kritérium splnění |
| --- | --- | --- |
| C1 | Umožnit uživateli registraci, přihlášení a odhlášení a rozlišit tři role. | Každá ze tří rolí vidí jen projekty, ke kterým má přístup, a testy oprávnění pro všechny role projdou. |
| C2 | Umožnit vedoucímu týmu vytvořit projekt a tým a spravovat jeho členy. | Tým lze založit, přidat do něj a odebrat z něj členy přímo v aplikaci, bez zásahu do databáze. |
| C3 | Umožnit vytvořit, upravit, smazat a přiřadit úkol s termínem a prioritou. | Úkol se všemi údaji se uloží a po obnovení stránky se zobrazí beze změny. |
| C4 | Zobrazit úkoly na Kanban nástěnce a umožnit jejich přesun mezi stavy. | Změna stavu se uloží do databáze. Při chybě se uživateli zobrazí hláška a úkol se vrátí do původního stavu. |
| C5 | Umožnit přidávat k úkolům komentáře a soubory. | Soubor do stanoveného limitu velikosti se nahraje a jde stáhnout. Větší soubor nebo nepovolený typ systém odmítne. |
| C6 | Zobrazit statistiky práce jednotlivých členů týmu. | U každého člena je vidět počet přiřazených, dokončených a zpožděných úkolů a čísla odpovídají stavu na nástěnce. |
| C7 | Poskytnout vyučujícímu přehled o jeho týmech. | Vyučující vidí všechny své týmy a stav jejich úkolů na jedné obrazovce, aniž by byl členem týmu. |

### Dílčí cíle: řízení projektu

| # | Dílčí cíl | Kritérium splnění |
| --- | --- | --- |
| C8 | Otestovat všechny funkce z rozsahu In Scope. | Ke každé oblasti In Scope existuje testovací scénář a všechny scénáře projdou před odevzdáním. |
| C9 | Zdokumentovat systém tak, aby ho šlo předat. | Dokumentace všech fází je odevzdaná v termínu a člen, který v týmu zůstane, dokáže systém podle README samostatně spustit. |
