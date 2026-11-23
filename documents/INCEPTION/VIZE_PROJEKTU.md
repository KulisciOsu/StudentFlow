## 4.1 Vize projektu

> **Krok 2 řetězce: vize.** Navazuje na [popis problému](PROBLEM_DESCRIPTION.md), pokračuje [stakeholdery](STAKEHOLDERS.md). Vizi rozvádějí [rozsah projektu](SCOPE_OF_PROJECT.md) a [cíle projektu](PROJECT_OBJECTIVES.md).

### Název projektu

**StudentFlow:** systém pro správu studentských týmových projektů

### Vize

Cílem projektu je vytvořit informační systém StudentFlow, který studentským týmům usnadní organizaci společné práce: rozdělování úkolů, sledování termínů a kontrolu průběhu projektu. Systém bude soustředit všechny důležité informace o projektu na jedno místo a zároveň průběžně ukazovat, jak se na práci podílejí jednotliví členové týmu. Díky tomu bude mít přehled nejen tým, ale i vyučující, který projekt hodnotí.

### Problém, který řešíme

**Problém:** Při práci na týmových projektech je obtížné udržet přehled o tom, kdo má jaký úkol, v jakém je stavu a do kdy má být dokončen. Informace bývají roztroušené po různých komunikačních platformách, dokumentech a poznámkách. Podrobně ho rozebírá [popis problému](PROBLEM_DESCRIPTION.md) (nedostatky N1 až N4).

**Koho se týká:** studentů pracujících v týmech, vedoucích týmů a vyučujících.

**Dopad:** Úkoly se ztrácejí nebo se na nich pracuje dvakrát, termíny se nestíhají a práce bývá rozdělená nerovnoměrně. Často také není jasné, kdo na projektu kolik odpracoval, což komplikuje férové hodnocení jednotlivých členů.

**Přínos řešení:** Tým má všechny úkoly, termíny a podklady na jednom místě, každý člen ví, co a do kdy má udělat, a vyučující vidí průběh projektu i podíl jednotlivých členů.

### Rozsah projektu

Systém bude obsahovat zejména:

- registraci, přihlášení a role uživatelů (člen týmu, vedoucí týmu, vyučující) s odpovídajícími oprávněními,
- vytváření a správu projektů,
- vytváření týmů a správu jejich členů,
- vytváření a přiřazování úkolů,
- Kanban nástěnku pro sledování stavu úkolů,
- nastavování termínů a priorit úkolů a plánování milníků projektu,
- komentáře a zpětnou vazbu k jednotlivým úkolům,
- přidávání souborů k úkolům a odevzdávání výstupů práce,
- oznámení o změnách přímo v aplikaci,
- přehled a statistiky práce jednotlivých členů týmu,
- přehled průběhu projektů pro vyučující.

Systém naopak nebude obsahovat:

- chat ani videohovory (komunikace zůstane v nástrojích, které týmy už používají),
- samostatnou mobilní aplikaci,
- napojení na školní informační systémy,
- e-mailová a push oznámení,
- samotné známkování (systém dává vyučujícímu podklady, hodnocení ale probíhá mimo něj).

Úplný výčet s odůvodněním je v [rozsahu projektu](SCOPE_OF_PROJECT.md).

### Cena a pracnost

Projekt vzniká jako školní projekt, a proto nemá stanovenou finanční cenu. Odhadovaná pracnost první části (zimní semestr 2026: Inception, Elaboration a 20 % implementace) je přibližně 400 hodin, tedy 5 členů × 6 hodin týdně × 13 týdnů. Pracnost zbylých 80 % se odhadne podle složení týmu v letním semestru, viz [plán projektu](../ELABORATION/PLAN_PROJEKTU.md).

### Stakeholdeři

Podrobný seznam s čísly S1 až S6 je v dokumentu [stakeholdeři](STAKEHOLDERS.md).

- Studenti pracující na týmových projektech (S1) potřebují vědět, jaké úkoly mají, do kdy je mají splnit a jak si stojí zbytek týmu.
- Vedoucí projektových týmů (S2) potřebují rozdělovat práci, hlídat termíny a mít přehled o stavu projektu.
- Vyučující (S3) potřebují sledovat průběh projektu a mít podklady pro férové hodnocení jednotlivých členů.
- Členové vývojového týmu (S4) potřebují jasně vymezené požadavky a rozsah, který lze v rámci předmětu realisticky zvládnout.

### Možné alternativy

Existují již nástroje pro správu projektů, například Trello, Jira, Asana nebo Microsoft Planner. Jira a Asana jsou výkonné, ale pro malé studentské týmy zbytečně složité. Trello a Microsoft Planner jsou jednodušší, ale všechny čtyři jsou obecné nástroje, které nejsou navržené pro školní prostředí, kde projekt hodnotí vyučující a potřebuje vidět průběh práce i podíl jednotlivých členů.

Pro studentské týmy, které potřebují mít práci na jednom místě a doložit podíl jednotlivých členů, je StudentFlow systém pro správu projektů, který spojuje úkoly, termíny a přehled příspěvků. Na rozdíl od uvedených nástrojů počítá s rolí vyučujícího, který průběh projektu sleduje a hodnotí.
