# Požadavky systému

## Funkční požadavky

**FP01 – Registrace a přihlášení:** Systém umožní uživatelům vytvořit účet, přihlásit se a odhlásit se.

**FP02 – Uživatelské role:** Systém bude rozlišovat role člena týmu, vedoucího týmu a vyučujícího s odpovídajícími oprávněními.

**FP03 – Správa projektů:** Systém umožní vytvářet a spravovat studentské projekty.

**FP04 – Správa týmů:** Systém umožní vytvářet týmy a spravovat jejich členy.

**FP05 – Správa úkolů:** Uživatelé s příslušným oprávněním budou moci vytvářet, upravovat a odstraňovat úkoly.

**FP06 – Přiřazování úkolů:** Vedoucí týmu bude moci přiřazovat úkoly jednotlivým členům.

**FP07 – Termíny a priority:** U jednotlivých úkolů bude možné nastavit termín dokončení a prioritu.

**FP08 – Kanban nástěnka:** Systém umožní zobrazovat úkoly podle jejich stavu a přesouvat je mezi jednotlivými stavy.

**FP09 – Komentáře:** Členové týmu a vyučující budou moci přidávat komentáře k úkolům.

**FP10 – Přílohy:** Systém umožní přikládat soubory k jednotlivým úkolům.

**FP11 – Přehled práce:** Systém zobrazí členům týmu jejich přiřazené úkoly a jejich aktuální stav.

**FP12 – Statistiky:** Systém poskytne přehled o aktivitě a plnění úkolů jednotlivých členů týmu.

**FP13 – Přístup vyučujícího:** Vyučující bude moci sledovat průběh studentských projektů.

## Nefunkční požadavky

**NP01 – Rychlost odezvy:** Systém musí při běžném zatížení zobrazit Kanban nástěnku a seznam úkolů do 2 sekund.

**NP02 – Plynulost ovládání:** Po přesunutí úkolu na Kanban nástěnce musí systém zobrazit jeho nový stav do 0,5 sekundy a uložit změnu do 2 sekund.

**NP03 – Bezpečnost a oprávnění:** Systém musí zabezpečit uživatelské účty a zajistit, aby uživatelé měli přístup pouze k projektům a funkcím odpovídajícím jejich oprávněním.

**NP04 – Integrita dat:** Systém musí zajistit správné a konzistentní ukládání změn úkolů, projektů a členství v týmech.

**NP05 – Práce se soubory:** Systém musí kontrolovat velikost a typ nahrávaných příloh. Maximální velikost jednoho souboru bude 10 MB.

**NP06 – Zpracování chyb:** Systém musí správně zpracovávat chybové stavy a srozumitelně informovat uživatele o neúspěšných operacích.

**NP07 – Kompatibilita:** Webová aplikace musí být funkční v aktuálních verzích běžných webových prohlížečů.

**NP08 – Použitelnost:** Uživatelské rozhraní musí být přehledné a umožňovat intuitivní správu úkolů, projektů a týmů bez nutnosti předchozího zaškolení.

**NP09 – Responzivita:** Uživatelské rozhraní musí být přizpůsobeno pro používání na počítačích, tabletech a mobilních zařízeních bez překrývání ovládacích prvků.

**NP10 – Spolehlivost:** Při selhání operace nesmí systém zobrazovat změny jako úspěšně uložené. Uživatel musí být informován o neúspěchu.
