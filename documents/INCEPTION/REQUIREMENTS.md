# 4.8 Požadavky systému

> **Krok 4 řetězce: požadavky.** Navazuje na [stakeholdery](STAKEHOLDERS.md) a [popis problému](PROBLEM_DESCRIPTION.md), pokračuje [případy užití](USE_CASE.md).

Každý požadavek uvádí, ze kterého očekávání stakeholdera (S1 až S6) a ze kterého nedostatku současného stavu (N1 až N4) vychází. Funkční požadavky dále odkazují na případy užití (UC), které je rozpracovávají. Které požadavky už pokrývá prototyp, uvádí [dokument o prototypu](../ELABORATION/PROTOTYP.md).

## Funkční požadavky

| # | Požadavek | Stakeholder | Problém | Use case |
| --- | --- | --- | --- | --- |
| FP01 | **Registrace a přihlášení:** Systém umožní uživatelům vytvořit účet, přihlásit se a odhlásit se. | S1, S2, S3 | N1 | UC12 |
| FP02 | **Uživatelské role:** Systém bude rozlišovat člena týmu, vedoucího týmu a vyučujícího s odpovídajícími oprávněními. Vyučující je typ účtu; vedoucí a člen jsou role v konkrétním projektu. | S1, S2, S3 | N3 | UC01, UC09, UC12, UC13; oprávnění platí ve všech UC |
| FP03 | **Správa projektů:** Systém umožní vytvářet projekty a upravovat jejich název, popis a termín. | S2 | N1 | UC04, UC14 |
| FP04 | **Správa týmů:** Systém umožní vytvořit tým spolu s projektem a spravovat jeho členy (přidat a odebrat člena). | S2 | N1 | UC04, UC13 |
| FP05 | **Správa úkolů:** Uživatelé s příslušným oprávněním budou moci vytvářet, upravovat a odstraňovat úkoly. | S1, S2 | N1, N2 | UC05 |
| FP06 | **Přiřazování úkolů:** Vedoucí týmu bude moci přiřazovat úkoly jednotlivým členům. Člen týmu může úkol přiřadit jen sám sobě. | S1, S2 | N1 | UC05 |
| FP07 | **Termíny a priority:** U jednotlivých úkolů bude možné nastavit termín dokončení a prioritu. | S2 | N2 | UC05, UC10 |
| FP08 | **Kanban nástěnka:** Systém umožní zobrazovat úkoly podle jejich stavu a přesouvat je mezi jednotlivými stavy. | S1, S2 | N2 | UC01, UC06 |
| FP09 | **Komentáře a zpětná vazba:** Členové týmu a vyučující budou moci přidávat komentáře k úkolům. Vyučující může posoudit odevzdaný výstup úkolu. | S1, S2, S3 | N1, N3 | UC02 |
| FP10 | **Přílohy a odevzdané výstupy:** Systém umožní přikládat soubory k projektům a úkolům a stahovat je. Opravený výstup úkolu se odevzdá jako nová verze, původní verze zůstane zachovaná. | S1, S2, S3 | N1, N4 | UC03, UC08 |
| FP11 | **Přehled práce:** Systém zobrazí členům týmu jejich projekty, přiřazené úkoly a jejich aktuální stav. | S1 | N2 | UC06 |
| FP12 | **Statistiky:** Systém poskytne přehled o aktivitě a plnění úkolů jednotlivých členů týmu. | S2, S3 | N4 | UC07 |
| FP13 | **Přístup vyučujícího:** Vyučující bude moci sledovat průběh projektů, ke kterým mu vedoucí týmu udělí přístup. | S2, S3 | N3 | UC06, UC09 |
| FP14 | **Plán a milníky:** Vedoucí týmu bude moci plánovat milníky projektu s termínem a souvisejícími úkoly. | S1, S2 | N2 | UC10 |
| FP15 | **Oznámení v aplikaci:** Systém zobrazí uživateli přímo v aplikaci oznámení o změnách, komentářích a blížících se termínech v jeho projektech. | S1, S2, S3 | N1, N2 | UC11 |

## Nefunkční požadavky

| # | Požadavek | Stakeholder |
| --- | --- | --- |
| NP01 | **Rychlost odezvy:** Systém musí při běžném zatížení zobrazit Kanban nástěnku a seznam úkolů do 2 sekund. | S1, S2 |
| NP02 | **Plynulost ovládání:** Po přesunutí úkolu na Kanban nástěnce musí systém zobrazit jeho nový stav do 0,5 sekundy a uložit změnu do 2 sekund. | S1 |
| NP03 | **Bezpečnost a oprávnění:** Systém musí zabezpečit uživatelské účty a zajistit, aby uživatelé měli přístup pouze k projektům a funkcím odpovídajícím jejich oprávněním. | S1, S3, S6 |
| NP04 | **Integrita dat:** Systém musí zajistit správné a konzistentní ukládání změn úkolů, projektů a členství v týmech. | S2, S4 |
| NP05 | **Práce se soubory:** Systém musí kontrolovat velikost a typ nahrávaných příloh. Maximální velikost jednoho souboru bude 20 MB. | S4, S6 |
| NP06 | **Zpracování chyb:** Systém musí správně zpracovávat chybové stavy a srozumitelně informovat uživatele o neúspěšných operacích. | S1, S4 |
| NP07 | **Kompatibilita:** Webová aplikace musí být funkční v aktuálních verzích běžných webových prohlížečů. | S1, S6 |
| NP08 | **Použitelnost:** Uživatelské rozhraní musí být přehledné a umožňovat intuitivní správu úkolů, projektů a týmů bez nutnosti předchozího zaškolení. | S1, S2 |
| NP09 | **Responzivita:** Uživatelské rozhraní musí být přizpůsobeno pro používání na počítačích, tabletech a mobilních zařízeních bez překrývání ovládacích prvků. | S1 |
| NP10 | **Spolehlivost:** Při selhání operace nesmí systém zobrazovat změny jako úspěšně uložené. Uživatel musí být informován o neúspěchu. | S1, S4 |
| NP11 | **Ochrana osobních údajů:** Systém ukládá jen údaje nutné pro provoz (jméno, e-mail, typ účtu). Při registraci vyžaduje potvrzení zásad ochrany osobních údajů a eviduje jejich verzi a čas potvrzení. | S6 |

Jak architektura jednotlivé nefunkční požadavky zajišťuje a čím se ověřují, uvádí [prototyp](../ELABORATION/PROTOTYP.md#ověření-architektury).

## Změny oproti první verzi

Požadavky jsou živý dokument. Při Elaboration (11/2026) je tým sladil s případy užití a wireframy:

- **Upřesněno:** FP02 (typ účtu proti roli v projektu), FP03 (úprava projektu), FP04 (správa členů), FP06 (člen přiřazuje úkol jen sobě), FP09 (posouzení výstupu vyučujícím), FP10 (verze odevzdaného výstupu), FP11 (přehled projektů), FP13 (přístup uděluje vedoucí týmu).
- **Doplněno:** FP14 a FP15, které vycházejí z existujících případů užití UC10 a UC11, a NP11 podle očekávání fakulty (S6).
- **Změněno:** limit souboru v NP05 z 10 MB na 20 MB podle rozhodnutí týmu.
