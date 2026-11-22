# Aktualizovaný plán projektu

> Výstup fáze Elaboration. Navazuje na [prototyp](PROTOTYP.md) a [risk-list](../INCEPTION/RISK_LIST.md).

Stav k 22. 11. 2026.

## Fáze podle OpenUP

| Fáze | Období | Otázka | Stav |
| --- | --- | --- | --- |
| Inception | říjen 2026 | Co chceme vytvořit a proč? Vize, problém, stakeholdeři, risk-list, rozsah, cíle, požadavky, případy užití, kandidát architektury. | Hotovo |
| Elaboration | konec října až prosinec 2026 | Jak bude systém fungovat? Architektura, technologie, wireframy, aktualizovaný risk-list, prototyp (20 % implementace). | Probíhá |
| Construction | letní semestr 2027 | Zbylých 80 % funkcí podle iterací níže. | Plán |
| Transition | konec letního semestru 2027 | Závěrečné testy, dokumentace a předání. | Plán |

## Do konce listopadu 2026: dokumentace a prezentace

| Úkol | Kdo | Stav |
| --- | --- | --- |
| Sloučit úpravy Vladimíra a větev KAN-140 (úklid kódu a dokumentace) | Vladimír Pospíšil, Marek Szczepanik | Probíhá |
| Spustit aplikaci podle návodu, chyby hlásit na Discordu | všichni | Po sloučení |
| Zkontrolovat dokumentaci podle zadání a doplnit návaznost, prototyp, plán a rozdělení práce | Adam Gebauer, kontrola všichni | Probíhá |
| Aktualizovaný risk-list: upravit stávající rizika a doplnit nová z Elaboration | Kryštof Kristuf | Probíhá |
| Zapsat ochranu osobních údajů (GDPR) do backlogu | Kryštof Kristuf | Probíhá |
| Registrace podle zpětné vazby (heslo, GDPR, texty), otestovat „Zůstat přihlášen“ | Kryštof Kristuf | Převážně hotovo (PR #19) |
| Základní branding | Adam Gebauer | Probíhá |
| Aktualizovat wireframy podle implementace | Adam Gebauer | Probíhá |
| Prezentace projektu podle zadání (kap. 9) | Adam Gebauer | Probíhá |

## Do konce prosince 2026: dokončení 20 %

| Úkol | Kdo | Vazba |
| --- | --- | --- |
| Doplnit kontrolu CSRF u založení projektu a přesunu úkolu | Marek Szczepanik, Vladimír Pospíšil | Omezení A02, NP03 |
| Srozumitelně odmítnout založení projektu vyučujícím, sjednotit pravidla formuláře a serveru | Vladimír Pospíšil | Omezení A03, A04 |
| Rozhodnout způsob ukládání souborů | vedoucí databáze a backendu | NP05, riziko R4 |
| Doplnit ER model o milníky, verze výstupů a historii změn | vedoucí databáze | [Mezery v návrhu](#mezery-v-návrhu) |
| Předávací schůzka: zůstávající člen samostatně spustí aplikaci | všichni | Cíl C10, riziko R2 |

## Mezery v návrhu

Wireframy pokrývají cílový stav všech případů užití. Datový návrh zatím pokrývá jen části, které tvoří prototyp:

- **ER model** zatím nemá entity pro milníky (UC10), verze odevzdaných výstupů a jejich posouzení (UC03) a historii změn úkolů (rozšíření UC01, UC07). Doplní je návrh databáze před implementací těchto případů užití.
- **Stavy úkolů:** ER model pracuje se společným číselníkem stavů, implementace má sloupce nástěnky pro každý projekt zvlášť, aby si je tým mohl upravit. ER model se při další revizi sladí s implementací.
- Rozhraní REST API pro případy užití z letního semestru se navrhne vždy na začátku příslušné iterace.

## Letní semestr 2027: iterace

Pořadí iterací určují závislosti mezi funkcemi a otevřená rizika. Složení týmu v letním semestru zatím není známé, proto se iterace rozdělí mezi řešitele na začátku semestru.

| Iterace | Obsah | Případy užití | Požadavky | Proč v tomto pořadí |
| --- | --- | --- | --- | --- |
| I1 | Správa úkolů a úprava projektu | UC05, UC14 | FP03, FP05 až FP07 | Úkoly jsou základ pro komentáře, soubory, statistiky i milníky. Nástěnka zatím pracuje s ukázkovými úkoly. |
| I2 | Soubory a odevzdané výstupy | UC03, UC08 | FP10, NP05 | Ošetří riziko R4, jediné zatím neošetřené riziko. |
| I3 | Přístup vyučujícího a zpětná vazba | UC09, UC06, UC02 | FP09, FP13 | Odstraní dočasné omezení A01 a doplní roli vyučujícího, která odlišuje StudentFlow od obecných nástrojů (N3). |
| I4 | Historie, statistiky a oznámení | rozšíření UC01, UC07, UC11 | FP12, FP15 | Statistiky a oznámení stojí na historii změn úkolů. |
| I5 | Plán a milníky | UC10 | FP14 | Nejmenší riziko, potřebuje hotové úkoly. |
| I6 | Dokončení a předání | | cíle C9 a C10 | Testy všech oblastí rozsahu, dokumentace a předání. |

Každá iterace končí sloučením do `main` s úspěšným během CI a aktualizací [prototypu](PROTOTYP.md) a [risk-listu](../INCEPTION/RISK_LIST.md).
