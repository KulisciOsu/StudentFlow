## 4.5 Rozsah projektu (Scope)

Do rozsahu patří deset oblastí funkcí, které tým dodá v rámci předmětu. Vše ostatní je mimo rozsah, dokud změnu tým výslovně neschválí.

### In Scope

| Oblast | Co systém umožní |
| --- | --- |
| **Uživatelé a přihlášení** | Registrace, přihlášení a odhlášení uživatele. |
| **Role a oprávnění** | Tři role (člen týmu, vedoucí týmu, vyučující). Každá role vidí a mění jen to, co jí přísluší, kontrola probíhá na backendu. |
| **Projekty** | Vytvoření projektu, úprava jeho názvu, popisu a termínu, přehled projektů. |
| **Týmy** | Vytvoření týmu, přidání a odebrání členů, určení vedoucího týmu. |
| **Úkoly** | Vytvoření, úprava a smazání úkolu, přiřazení členovi týmu, nastavení termínu a priority. |
| **Kanban nástěnka** | Zobrazení úkolů ve sloupcích podle stavu (např. K řešení, Rozpracováno, Hotovo) a jejich přesun mezi stavy přetažením (Drag & Drop). |
| **Komentáře** | Přidání a zobrazení komentářů u jednotlivých úkolů. |
| **Soubory** | Nahrání souboru k úkolu a jeho stažení, s omezením velikosti a povolených typů. |
| **Statistiky členů** | Přehled přiřazených, dokončených a zpožděných úkolů jednotlivých členů týmu. |
| **Přehled pro vyučujícího** | Seznam týmů a projektů vyučujícího se stavem úkolů a statistikami členů. |

### Out of Scope

| Funkce | Proč není součástí projektu |
| --- | --- |
| **Chat a videohovory** | Týmy už komunikují ve vlastních nástrojích (Discord, Teams), další chat by nepřinesl novou hodnotu. |
| **Samostatná mobilní aplikace** | Systém bude webová aplikace v prohlížeči, mobilní aplikace by zdvojila vývoj. |
| **Napojení na školní informační systémy a přihlášení školním účtem** | Vyžaduje přístup a souhlas školy, který tým v rámci předmětu nemá. |
| **Známkování** | Systém dává vyučujícímu podklady, samotné hodnocení probíhá mimo něj. |
| **E-mailové a push notifikace** | Nad rámec časových možností týmu, termíny a stav úkolů jsou vidět přímo v systému. |
| **Napojení na GitHub, Jiru a další externí nástroje** | Integrace jsou časově náročné a pro hlavní cíl projektu nejsou nutné. |
| **Pokročilé řízení projektů** (Ganttův diagram, závislosti mezi úkoly, sprinty) | Právě tyto funkce dělají obecné nástroje pro studentské týmy zbytečně složité. |
| **Úprava a verzování souborů v systému** | Soubory se pouze nahrávají a stahují, upravují se v nástrojích, kde vznikly. |

### Změny rozsahu

Každý nový požadavek tým nejdřív zapíše do Jiry jako návrh. Do rozsahu se dostane, jen když ho na týmové schůzce schválí vedoucí týmu a nezpůsobí ohrožení termínu (riziko R1). Neschválené nápady se odkládají do seznamu možných rozšíření.

---
