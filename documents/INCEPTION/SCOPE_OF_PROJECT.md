## 4.5 Rozsah projektu (Scope)

> **Součást kroku 2 řetězce: vize.** Rozvádí [vizi projektu](VIZE_PROJEKTU.md) do konkrétních oblastí. Každá oblast odkazuje na [požadavky](REQUIREMENTS.md) (FP) a [případy užití](USE_CASE.md) (UC).

Do rozsahu patří dvanáct oblastí funkcí, které tým dodá v rámci předmětu. Vše ostatní je mimo rozsah, dokud změnu tým výslovně neschválí.

### In Scope

| Oblast | Co systém umožní | Požadavky | Use case |
| --- | --- | --- | --- |
| **Uživatelé a přihlášení** | Registrace, přihlášení a odhlášení uživatele, potvrzení zásad ochrany osobních údajů. | FP01, NP11 | UC12 |
| **Role a oprávnění** | Tři role (člen týmu, vedoucí týmu, vyučující). Každá role vidí a mění jen to, co jí přísluší, kontrola probíhá na backendu. | FP02, NP03 | všechny |
| **Projekty** | Vytvoření projektu, úprava jeho názvu, popisu a termínu, přehled projektů. | FP03, FP11 | UC04, UC06, UC14 |
| **Týmy** | Vytvoření týmu, přidání a odebrání členů, určení vedoucího týmu. | FP04 | UC04, UC13 |
| **Úkoly** | Vytvoření, úprava a smazání úkolu, přiřazení členovi týmu, nastavení termínu a priority. | FP05, FP06, FP07 | UC05 |
| **Kanban nástěnka** | Zobrazení úkolů ve sloupcích podle stavu (např. K řešení, Rozpracováno, Hotovo) a jejich přesun mezi stavy přetažením (Drag & Drop). | FP08 | UC01, UC06 |
| **Plán a milníky** | Milníky projektu s termínem a souvisejícími úkoly. | FP14 | UC10 |
| **Komentáře a zpětná vazba** | Přidání a zobrazení komentářů u jednotlivých úkolů, posouzení odevzdaného výstupu vyučujícím. | FP09 | UC02 |
| **Soubory** | Nahrání souboru k projektu nebo úkolu a jeho stažení, s omezením velikosti (20 MB) a povolených typů. Opravený výstup úkolu se odevzdá jako nová verze. | FP10, NP05 | UC03, UC08 |
| **Oznámení v aplikaci** | Přehled oznámení o změnách, komentářích a termínech, označení jako přečtené. | FP15 | UC11 |
| **Statistiky členů** | Přehled přiřazených, dokončených a zpožděných úkolů jednotlivých členů týmu. | FP12 | UC07 |
| **Přehled pro vyučujícího** | Seznam projektů, ke kterým má vyučující přístup, se stavem úkolů a statistikami členů. | FP13 | UC06, UC09 |

### Out of Scope

| Funkce | Proč není součástí projektu |
| --- | --- |
| **Chat a videohovory** | Týmy už komunikují ve vlastních nástrojích (Discord, Teams), další chat by nepřinesl novou hodnotu. |
| **Samostatná mobilní aplikace** | Systém bude webová aplikace v prohlížeči, mobilní aplikace by zdvojila vývoj. Webová aplikace je přizpůsobená i pro mobil (NP09). |
| **Napojení na školní informační systémy a přihlášení školním účtem** | Vyžaduje přístup a souhlas školy, který tým v rámci předmětu nemá. |
| **Známkování** | Systém dává vyučujícímu podklady a zpětnou vazbu, samotné hodnocení probíhá mimo něj. |
| **E-mailové a push notifikace** | Nad rámec časových možností týmu. Oznámení se zobrazují jen přímo v aplikaci (UC11). |
| **Napojení na GitHub, Jiru a další externí nástroje** | Integrace jsou časově náročné a pro hlavní cíl projektu nejsou nutné. |
| **Pokročilé řízení projektů** (Ganttův diagram, závislosti mezi úkoly, sprinty) | Právě tyto funkce dělají obecné nástroje pro studentské týmy zbytečně složité. Postačí milníky (UC10). |
| **Úprava souborů přímo v systému** | Soubory se pouze nahrávají a stahují a upravují se v nástrojích, kde vznikly. Opravu výstupu tým nahraje jako novou verzi (UC03). |
| **Administrátorská role** | Systém spravují vedoucí týmů. Samostatný administrátor by přidal další roli a oprávnění bez přínosu pro hlavní cíl. |

### Etapy dodání

Rozsah se dodává ve dvou etapách podle předmětu. Do konce prosince 2026 tým dodá **20 %** implementace: jádro architektury a funkce, které ověřují největší technická rizika. Zbylých **80 %** dodá v letním semestru. Co přesně obsahuje první etapa, popisuje [prototyp](../ELABORATION/PROTOTYP.md), další postup [plán projektu](../ELABORATION/PLAN_PROJEKTU.md).

### Změny rozsahu

Každý nový požadavek tým nejdřív zapíše do Jiry jako návrh. Do rozsahu se dostane, jen když ho na týmové schůzce schválí vedoucí týmu a nezpůsobí ohrožení termínu (riziko R1). Neschválené nápady se odkládají do seznamu možných rozšíření.

Při Elaboration (11/2026) tým rozsah sladil s případy užití a wireframy: přibyly oblasti **Plán a milníky** a **Oznámení v aplikaci** a verze odevzdaných výstupů. Administrátorská role byla výslovně vyřazena.
