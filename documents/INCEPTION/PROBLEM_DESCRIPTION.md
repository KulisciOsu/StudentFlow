## 4.2 Popis problému

> **Krok 1 řetězce: problém.** Pokračuje [vizí projektu](VIZE_PROJEKTU.md).

Studentské týmy dnes organizují společnou práci v několika nesouvisejících nástrojích, takže nikdo nemá úplný přehled o úkolech, termínech a podílu jednotlivých členů. Nejvíc to pocítí vyučující, který má hodnotit jednotlivce, ale vidí hlavně výsledek práce celého týmu.

### Současný stav

V mnoha předmětech studenti zpracovávají semestrální projekt v týmu. Projekt trvá obvykle celý semestr, má průběžné kontrolní body a na konci ho hodnotí vyučující. Každý tým si sám volí, jak bude komunikovat a jak si rozdělí práci. Škola k tomu žádný jednotný nástroj neposkytuje.

### Problém

Tým nemá jedno místo, kde by byly úkoly, jejich stav, termíny a podklady pohromadě. Informace o tom, kdo na čem pracuje, existují hlavně v konverzacích a v hlavách jednotlivých členů. Vyučující vidí většinou až výsledek, ne cestu k němu, a nemá tak spolehlivé podklady pro hodnocení jednotlivců.

### Koho problém ovlivňuje

- **Členové týmu** nevědí vždy přesně, co a do kdy mají udělat, a jejich práce nemusí být vidět.
- **Vedoucí týmu** musí stav úkolů zjišťovat ručně, připomínat termíny a hlídat, aby se práce nedublovala.
- **Vyučující** hodnotí tým jako celek, i když se jeho členové na práci podíleli velmi různě.

### Jak se problém řeší dnes

- **Chatovací aplikace** (Discord, WhatsApp, Messenger, Microsoft Teams) slouží k domlouvání i k zadávání úkolů.
- **Sdílené dokumenty a tabulky** (Google Docs, Google Sheets, OneDrive) nahrazují seznam úkolů.
- **Obecné nástroje pro správu projektů** (Trello, Jira, Asana, Microsoft Planner) používá jen část týmů, každý jiný.
- **Vyučující** zjišťuje průběh na konzultacích a prezentacích, z vlastního hodnocení studentů nebo z historie změn v kódu, například na GitHubu.

### Nedostatky současného řešení

Každý nedostatek má číslo, na které odkazují další dokumenty.

| # | Nedostatek | Popis |
| --- | --- | --- |
| N1 | **Roztříštěnost** | Informace jsou ve více nástrojích najednou a úkol zadaný v chatu snadno zapadne. |
| N2 | **Chybí aktuální přehled** | Tabulky s úkoly se přestávají aktualizovat a stav práce není vidět bez ptaní. |
| N3 | **Obecné nástroje nesedí na školu** | Jira a Asana jsou na nastavení složité a žádný z nich nepočítá s rolí vyučujícího. Ten by musel být pozván do nástroje každého týmu zvlášť. |
| N4 | **Podíl jednotlivců nejde doložit** | Historie kódu ukazuje jen programování, ne analýzu, dokumentaci nebo testování. Hodnocení pak stojí na dojmu nebo na tom, co tým řekne sám. |

### Proč je vhodné vytvořit nový systém

Nový systém spojí úkoly, termíny, komentáře a soubory na jedno místo (N1) a bude obsahovat jen funkce, které studentský tým opravdu potřebuje (N3). Stav úkolů bude vidět na nástěnce bez ptaní (N2). Role vyučujícího bude součástí systému od začátku, takže uvidí všechny své týmy v jednom prostředí (N3). Statistiky práce jednotlivých členů budou vycházet ze skutečně přiřazených a dokončených úkolů, ne z dojmu (N4). Takový nástroj dnes žádná z uvedených alternativ nenabízí.
