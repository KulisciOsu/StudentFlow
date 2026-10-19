# Use Case model




# Přehled případů užití (Use Cases)

## UC01 Aktualizovat stav úkolu
* **Role:** Člen týmu, vedoucí týmu
* **Cíl:** Sdílet aktuální průběh práce na přiděleném úkolu.
* **Vstup:** Úkol existuje. Člen mění svůj přidělený úkol; vedoucí kterýkoli úkol svého týmu.
* **Průběh:**
  1. Uživatel otevře úkol na nástěnce a zvolí nový stav.
  2. Systém ověří oprávnění a povolený přechod.
  3. Uloží stav s autorem a časem, aktualizuje nástěnku a případně vytvoří oznámení.
* **Výjimky:** Nepovolený přechod nebo úpravu cizího úkolu systém odmítne. Při souběžné změně upozorní na konflikt a umožní načíst aktuální verzi.
* **Výsledek:** Tým vidí nový stav a jeho historii; tím se neuděluje přijetí řešení.

---

## UC02 Přidat komentář nebo zpětnou vazbu
* **Role:** Člen týmu, vedoucí týmu, vyučující
* **Cíl:** Uchovat dotaz nebo připomínku u správného projektu, úkolu či verze řešení.
* **Vstup:** Uživatel má přístup k cílovému objektu. Vyučující je přiřazen k projektu.
* **Průběh:**
  1. Uživatel otevře projekt, úkol nebo konkrétní verzi a napíše komentář.
  2. Vyučující může k připomínce doplnit prioritu nebo termín opravy; jde o údaj zpětné vazby.
  3. Systém ověří vstup a přístup, uloží autora, čas a vazbu na objekt; komentář zobrazí oprávněným uživatelům a případně je upozorní.
* **Výjimky:** Prázdný nebo příliš dlouhý text se odmítne. Při ztrátě přístupu se komentář neuloží. Uživatelský vstup se bezpečně zobrazí.
* **Výsledek:** Je zřejmé, čeho se komentář týká; komentář sám nemění zadání ani stav úkolu.

---

## UC03 Přiložit soubor nebo výstup práce
* **Role:** Člen týmu, vedoucí týmu
* **Cíl:** Sdílet podklady projektu a odevzdat konkrétní výstup úkolu.
* **Vstup:** Uživatel má právo přidat přílohu k projektu nebo úkolu.
* **Průběh:**
  1. Uživatel vybere soubor u projektu nebo úkolu. U odevzdání řešení vybere příslušný úkol.
  2. Systém ověří přístup, povolený typ a maximální velikost souboru.
  3. Uloží přílohu; odevzdaný výstup úkolu eviduje jako samostatnou verzi s autorem, časem a počátečním posouzením Neposouzeno.
* **Výjimky:** Nevhodný soubor odmítne. Neúspěšný přenos nevytvoří dostupnou přílohu a umožní opakování. Oprava je novou verzí.
* **Výsledek:** Soubor lze stáhnout podle UC09. Původní verze i její komentáře a posouzení zůstávají zachovány.

---

## UC04 Založit týmový projekt
* **Role:** Přihlášený člen (založením se stává vedoucím)
* **Cíl:** Vytvořit společný prostor pro studentský projekt a jeho tým.
* **Vstup:** Student má účet a možnost založit projekt.
* **Průběh:**
  1. Student zadá název, popis a plánovaný termín projektu.
  2. Vyplní název nového týmu a potvrdí vytvoření.
  3. Systém ověří údaje, společně vytvoří projekt a tým, přiřadí zakladatele jako vedoucího a zobrazí prázdnou nástěnku.
* **Výjimky:** Chybějící či neplatné údaje musí uživatel opravit. Při selhání ukládání nesmí vzniknout neúplný projekt bez týmu.
* **Výsledek:** Projekt je propojený s týmem a jeho vedoucím.

---

## UC05 Spravovat a přiřazovat úkoly
* **Role:** Člen a vedoucí týmu
* **Cíl:** Rozdělit práci s jasným zadáním, odpovědností, termínem a prioritou.
* **Vstup:** Člen a vedoucí mohou spravovat příslušný projekt.
* **Průběh:**
  1. Zvolí vytvoření nového úkolu nebo úpravu existujícího.
  2. Zadá či upraví název, popis, termín, prioritu a řešitele z vlastního týmu.
  3. Systém ověří povinné údaje a členství, uloží změnu a zobrazí úkol na nástěnce; nový úkol má výchozí stav.
* **Výjimky:** Úkol nelze přiřadit člověku mimo tým. Neplatné nebo chybějící povinné údaje brání uložení.
* **Výsledek:** Úkol je vytvořen či upraven a má stanovené podmínky plnění. Samostatná změna stavu patří do UC01.

---

## UC06 Zobrazit projekt a jeho průběh
* **Role:** Člen týmu, vedoucí týmu, vyučující
* **Cíl:** Zjistit zadání projektu, stav práce, termíny a případná zpoždění.
* **Vstup:** Uživatel má přístup k projektu; pro vyučujícího platí vazba podle UC09.
* **Průběh:**
  1. Uživatel otevře seznam dostupných projektů a vybere projekt.
  2. Systém zobrazí základní informace, členy, Kanban úkolů, milníky, termíny a aktivitu; zvýrazní dostupné informace o nedokončené či opožděné práci.
  3. Uživatel filtruje úkoly nebo otevře detail s řešitelem, prioritou, přílohami, verzemi, komentáři a posouzeními včetně vyučujícího.
* **Výjimky:** Prázdný projekt zobrazí prázdný přehled. Neexistující či nepřístupný projekt se nezpřístupní ani přímým zadáním adresy.
* **Výsledek:** Uživatel má aktuální přehled. Zobrazení samo nemění projektová data.

---

## UC07 Přehledová statistika
* **Role:** Člen týmu, vedoucí týmu, vyučující
* **Cíl:** Doložit rozdělení práce a zapojení jednotlivých členů týmu.
* **Vstup:** Systém eviduje autorství a čas relevantních událostí projektu.
* **Průběh:**
  1. Uživatel otevře přehled příspěvků a zvolí časové období.
  2. Systém zobrazí úkoly členů a události přiřazené autorům, zejména komentáře, přílohy a změny stavů.
  3. Uživatel vybere člena a dohledá konkrétní úkoly a události, ze kterých souhrn vznikl.
* **Výjimky:** Období bez událostí má nulové hodnoty. Příspěvky bývalých členů zůstávají dohledatelné.
* **Výsledek:** Souhrn je ověřitelný z podkladů. Počet akcí nevyjadřuje hodiny ani kvalitu práce a neurčuje známku.

---

## UC08 Stáhnout přílohu
* **Role:** Člen týmu, vedoucí týmu, vyučující
* **Cíl:** Získat sdílený soubor nebo konkrétní verzi výstupu pro práci či kontrolu.
* **Vstup:** Příloha existuje a uživatel má přístup k jejímu projektu nebo úkolu. U úkolu nezáleží na jeho Kanban stavu.
* **Průběh:**
  1. Uživatel otevře seznam příloh projektu nebo detailu úkolu.
  2. Vybere přílohu či konkrétní verzi řešení a zvolí stažení.
  3. Systém znovu ověří přístup k objektu i souboru a odešle soubor do zařízení uživatele.
* **Výjimky:** Bez příloh se zobrazí prázdný seznam. Neoprávněný přímý požadavek se odmítne; nedostupný nebo neotevíratelný výstup vyvolá chybu a možnost opakování.
* **Výsledek:** Soubor je stažen. Jeho uložený obsah ani stav úkolu se nemění.

---

## UC09 Zpřístupnit projekt vyučujícímu
* **Role:** Vedoucí týmu
* **Cíl:** Udělit nebo odebrat konkrétnímu vyučujícímu přístup k projektu.
* **Vstup:** Vedoucí spravuje vlastní projekt; administrátor může vybrat projekt v systému. Cílový účet má systémovou roli vyučujícího.
* **Průběh:**
  1. Vedoucí otevře nastavení svého projektu.
  2. Vybere vyučujícího a potvrdí udělení či odebrání přístupu.
  3. Systém ověří oprávnění a aktualizuje vazbu. Udělení zpřístupní přehledy, komentáře, soubory a posuzování řešení; odebrání přístup odvolá.
* **Výjimky:** Účet bez role vyučujícího nelze přiřadit. Duplicitní vazba nevznikne. Běžný člen ani vyučující přístup nespravují; vedoucí nespravuje cizí projekt.
* **Výsledek:** Přístup vyučujícího je aktualizovaný. Administrátor tím nezískává právo číst obsah projektu.

---

## UC10 Plánovat deadline
* **Role:** Vedoucí týmu
* **Cíl:** Stanovit kontrolní body a termíny důležitých částí projektu.
* **Vstup:** Projekt existuje a vedoucí má právo jej upravovat.
* **Průběh:**
  1. Vedoucí otevře plán projektu a vytvoří nebo vybere milník.
  2. Zadá či upraví název, popis, termín a případně související úkoly.
  3. Systém ověří údaje, uloží milník a zobrazí jej v plánu; případně upozorní členy týmu.
* **Výjimky:** Neplatný termín nebo rozpor s pravidly projektu vyžaduje opravu. Kolize s jiným plánem může vyvolat upozornění; zdroj neurčuje povinné blokování.
* **Výsledek:** Milník a termín jsou dostupné oprávněným uživatelům v přehledu projektu.

---

## UC11 Zobrazit a číst oznámení
* **Role:** Člen týmu, vedoucí týmu, vyučující
* **Cíl:** Získat přehled o změnách, komentářích, termínech a výsledcích kontrol.
* **Vstup:** Uživatel má oznámení odpovídající jeho projektům a oprávněním.
* **Průběh:**
  1. Uživatel otevře centrum oznámení.
  2. Systém zobrazí relevantní oznámení v rozsahu aktuálních oprávnění.
  3. Uživatel otevře oznámení, přejde na související projekt či úkol a může jej označit jako přečtené.
* **Výjimky:** Bez nových oznámení se zobrazí informace, že je vše aktuální. Odkaz nesmí obejít kontrolu přístupu k cílovému objektu.
* **Výsledek:** Uživatel se dostane k související aktivitě a může evidovat přečtení. Kanály a pravidla automatického doručování zdroje neurčují.


# Seznam hlavních případů užití

| ID | Případ užití | Člen | Vedoucí | Vyučující |
| :--- | :--- | :---: | :---: | :---: |
| UC01 | Aktualizovat stav úkolu | Ano | Ano | - |
| UC02 | Přidat komentář nebo zpětnou vazbu | Ano | Ano | Ano |
| UC03 | Přiložit soubor nebo výstup práce | Ano | Ano | - |
| UC04 | Založit týmový projekt | - | Ano | - |
| UC05 | Spravovat a přiřazovat úkoly | Ano | Ano | - |
| UC06 | Zobrazit projekt a jeho průběh | Ano | Ano | Ano |
| UC07 | Přehledová statistika | Ano | Ano | Ano |
| UC08 | Stáhnout přílohu | Ano | Ano | Ano |
| UC09 | Zpřístupnit projekt vyučujícímu | - | Ano | - |
| UC10 | Plánovat deadline | - | Ano | - |
| UC11 | Zobrazit a číst oznámení | Ano | Ano | Ano |


# User Stories

## UC01 Aktualizovat stav úkolu
- Jako člen týmu chci změnit stav svého přiděleného úkolu, abych informoval ostatní členy o aktuálním průběhu své práce.
- Jako vedoucí týmu chci měnit stav libovolného úkolu svého týmu, abych mohl koordinovat průběh projektu a udržovat aktuální přehled o práci.
- Jako člen týmu nebo vedoucí týmu chci vidět historii změn stavu úkolů včetně autora a času změny, abych mohl zpětně sledovat průběh práce.

## UC02 Přidat komentář nebo zpětnou vazbu
- Jako člen týmu chci přidat komentář k projektu, úkolu nebo konkrétní verzi řešení, abych mohl sdílet dotazy, informace a připomínky s ostatními oprávněnými uživateli.
- Jako vyučující přiřazený k projektu chci přidat komentář ke konkrétnímu projektu, úkolu nebo verzi řešení, abych mohl studentům poskytnout zpětnou vazbu k jejich práci.

## UC03 Přiložit soubor nebo výstup práce
- Jako člen týmu chci přiložit soubor k projektu, abych mohl sdílet podklady potřebné pro společnou práci.
- Jako člen týmu chci odevzdat výstup ke konkrétnímu úkolu, abych mohl předat svou práci týmu a vyučujícímu ke kontrole.
- Jako člen týmu chci při opravě odevzdaného řešení vytvořit novou verzi, abych mohl zach

## UC04 Založit týmový projekt
- Jako přihlášený člen týmu chci založit nový projekt a vytvořit k němu tým, abych mohl zahájit spolupráci na studentském projektu.
- Jako člen týmu zakládající projekt chci po jeho vytvoření získat roli vedoucího týmu, abych mohl organizovat členy, úkoly a další činnosti projektu.

## UC05 Spravovat a přiřazovat úkoly
- Jako člen týmu nebo vedoucí týmu chci vytvořit úkol s názvem, popisem, termínem, prioritou a řešitelem, abych mohl rozdělit práci na konkrétní činnosti.
- Jako člen týmu nebo vedoucí týmu chci upravit údaje existujícího úkolu, abych mohl reagovat na změny požadavků a organizace práce.
- Jako člen týmu nebo vedoucí týmu chci přiřadit úkol konkrétnímu členovi vlastního týmu, abych mohl určit odpovědnost za jeho splnění.

## UC06 Zobrazit projekt a jeho průběh
- Jako člen týmu chci zobrazit informace o projektu, jeho členech, úkolech a termínech, abych věděl, jak projekt postupuje a jaké činnosti mě čekají.
- Jako vedoucí týmu chci zobrazit stav úkolů, deadline a aktivitu členů, abych mohl sledovat průběh projektu a identifikovat nedokončenou nebo opožděnou práci.
- Jako vyučující s přiděleným přístupem chci zobrazit průběh projektu a jeho výstupy, abych mohl sledovat práci studentů a kontrolovat odevzdaná řešení.

## UC07 Přehledová statistika
- Jako člen týmu chci zobrazit přehled příspěvků členů za zvolené období, abych získal představu o rozdělení práce v týmu.
- Jako vedoucí týmu chci sledovat úkoly a zaznamenané aktivity jednotlivých členů, abych mohl doložit jejich zapojení do projektu.
- Jako vyučující s přístupem k projektu chci zobrazit přehled příspěvků členů týmu, abych mohl získat podklady o průběhu jejich práce.

## UC08 Stáhnout přílohu
- Jako člen týmu chci stáhnout soubor přiložený k projektu, abych mohl využít sdílené podklady při práci.
- Jako člen týmu nebo vedoucí týmu chci stáhnout konkrétní verzi řešení úkolu, abych mohl pracovat s odevzdaným výstupem nebo porovnat jeho verze.
- Jako vyučující s přístupem k projektu chci stáhnout konkrétní verzi odevzdaného řešení, abych ji mohl zkontrolovat a posoudit.

## UC09 Zpřístupnit projekt vyučujícímu
- Jako vedoucí týmu chci přidělit konkrétnímu vyučujícímu přístup ke svému projektu, abych mu umožnil sledovat průběh práce, prohlížet výstupy a poskytovat zpětnou vazbu.
- Jako vedoucí týmu chci odebrat vyučujícímu přístup ke svému projektu, abych zajistil, že k němu již nebude mít oprávnění.

## UC10 Plánovat deadline
- Jako vedoucí týmu chci vytvořit deadline s názvem, popisem a termínem, abych stanovil kontrolní bod pro důležitou část projektu.
- Jako vedoucí týmu chci upravit termín deadline a přiřadit k němu související úkoly, abych mohl koordinovat jednotlivé části projektu.
- Jako člen týmu chci vidět deadline v přehledu projektu, abych věděl, do kdy mají být důležité části práce dokončeny.

## UC11 Zobrazit a číst oznámení
- Jako člen týmu chci zobrazit oznámení týkající se mých projektů a úkolů, abych měl přehled o změnách, komentářích a blížících se termínech.
- Jako vedoucí týmu chci dostávat přehled relevantních oznámení o aktivitách v projektu, abych mohl včas reagovat na důležité změny a výsledky kontrol.
- Jako vyučující s přístupem k projektu chci zobrazovat oznámení související s projekty, které kontroluji, abych mohl sledovat relevantní aktivitu a výsledky posouzení.

