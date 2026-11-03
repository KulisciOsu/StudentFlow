# KAN-114, KAN-101, KAN-102: API a doplňky wireframů autentizace

Podklady a implementace pro
[KAN-46](https://kulisciosu.atlassian.net/browse/KAN-46): registrace,
login, `/me`, logout, ochrana stránek a navigace podle jediného typu účtu.

Registrace přechází na login s potvrzením a e-mailem předaným pouze v paměti.
Session platí 8 hodin, s „Zůstat přihlášen“ 30 dní, bez prodlužování.
KAN-103/104/105/107/116 jsou implementované v rozsahu schváleného kontraktu.
Testy KAN-111/112 zahrnují i negativní a výpadkové scénáře.
Tabletová varianta má širší přihlašovací kartu, registraci ve dvou sloupcích
a hlavičku přehledu přizpůsobenou orientaci zařízení. Podrobnosti jsou
v [pravidlech responzivity](SCREENS_AND_FLOWS.md#responzivita-a-přístupnost).

**Zdrojem pravdy pro stávající obrazovky je původní
[Wireframe.pdf](../Wireframe.pdf).** Přihlášení je na straně 3 a registrace
na straně 4. Podklady v této složce doplňují mobilní autentizaci, odhlášení
a podrobné stavy session, které původní dokument nepokrývá.

| Výstup | Obsah |
| :--- | :--- |
| [API kontrakt](API_CONTRACT.md) | Endpointy, validace, session, chyby a předání identity dashboardu KAN-47. |
| [Integrační návod KAN-115](INTEGRATION_GUIDE.md) | Spuštění, chráněný endpoint, nová stránka, CSRF a převzetí jiným vývojářem. |
| [OpenAPI 3.1.1](auth.openapi.yaml) | Strojově čitelná specifikace téhož rozhraní. |
| [Stavy a přechody](SCREENS_AND_FLOWS.md) | Textová upřesnění chování formulářů, navigace, chyb a scénářů k ověření. |
| [PDF doplňků](auth-wireframes.pdf) | Dvě strany: mobilní autentizace a podrobné stavy včetně odhlášení. |
| Upravitelné doplňky | [Mobil](auth-mobile.svg), [stavy a odhlášení](auth-states.svg). |

## Pravidla vycházející ze zadání

- Zachovat React, TypeScript, Vite, ASP.NET Core, PostgreSQL a Dapper.
- Účet má jméno, unikátní e-mail, heslo a právě jeden typ: **Člen**, nebo **Vyučující**.
- E-mail se ořízne a převede na malá písmena; heslo má 15–128 znaků.
- Zabezpečená cookie a serverová session: 8 hodin, s volbou „Zůstat přihlášen“
  30 dní, bez automatického prodlužování. Logout session zneplatňuje na serveru.
- Po registraci následuje přihlášení. Člen začíná na `/projects`, vyučující
  na `/teaching`. Přepínání mezi typy účtu se nenabízí.
- Vedení projektu vyplývá z členství. Typ účtu sám neposkytuje přístup k projektům.
- Rozhraní funguje od 320 CSS px, dotykem i klávesnicí; chybu nevydává za úspěch.

Zdroje: [KAN-46](https://kulisciosu.atlassian.net/browse/KAN-46),
[KAN-101](https://kulisciosu.atlassian.net/browse/KAN-101),
[KAN-102](https://kulisciosu.atlassian.net/browse/KAN-102),
[KAN-103](https://kulisciosu.atlassian.net/browse/KAN-103),
[KAN-105](https://kulisciosu.atlassian.net/browse/KAN-105),
[KAN-114](https://kulisciosu.atlassian.net/browse/KAN-114).

Právě jeden typ účtu nahrazuje původní možnost obou typů z KAN-46/KAN-105.
Ve formuláři se používá označení
„Člen“; existující technický kód `STUDENT` se zachovává. Samotná registrace člena
jej ještě nepřidává do konkrétního týmu nebo projektu.

## Schválená rozhodnutí

1. **Povinná volba jednoho typu účtu pomocí radio tlačítek** „Člen“ a „Vyučující“.
   Výchozí formulář nemá nic vybráno; volba jedné možnosti ruší druhou.
   Samoregistrace vyučujícího vychází ze zadání.
2. **Heslo zadat dvakrát**, každé pole s přepínačem viditelnosti. Opakování
   kontroluje frontend a neposílá je API.
3. **Po registraci přejít na přihlášení** s potvrzením a předvyplněným e-mailem
   v paměti aplikace. Heslo se nepřenáší ani neuchovává.
4. **Obnova hesla není součástí tohoto průchodu.** Odkaz z původního PDF se zatím
   nezobrazí; nejsou zadané endpointy ani způsob bezpečné obnovy.
5. **Frontend i API ze stejného originu**, ve vývoji přes Vite proxy `/api`.
   CSRF token ze samostatného endpointu se uchovává jen v paměti.
6. **Po loginu výchozí rozcestník podle typu účtu.** Automatický návrat na
   původní hluboký odkaz zatím není součástí návrhu; přímé otevření chráněné
   adresy s platnou session ovšem musí fungovat.

## Návaznost na současný návrh a kód

Původní `Wireframe.pdf` určuje podobu již navržených desktopových obrazovek:
přihlášení a registrace (strany 3–4), přehled člena (strana 5) a přehled
vyučujícího (strana 7). Původní registrace již obsahuje volbu jednoho typu
účtu a přihlášení volbu „Zůstat přihlášen“.

| Doplněk | Co původní wireframe nepokrývá |
| :--- | :--- |
| `auth-mobile.svg` | Přihlášení a registrace na mobilu; původní mobilní strana 16 řeší nástěnku a detail úkolu. |
| `auth-states.svg` | Odhlášení, expirace, načítání identity, výpadky a další výsledky autentizačních operací. Hlavička ilustruje doplněné ovládání autentizace. |
| `auth-wireframes.pdf` | Export těchto dvou doplňků ze SVG. |

Duplicitní desktopový návrh byl podle pokynu uživatele odstraněn spolu
s jeho stránkou v exportu. Doplňky rozšiřují nepokryté chování a šířky;
nenahrazují původní wireframy.

Již schválená upřesnění se evidují textově: popisek „Člen“ pro `STUDENT`,
heslo 15–128 znaků oproti původní nápovědě 8 znaků, přepínání viditelnosti
hesla, životnost session a současný rozsah bez obnovy hesla. Pravidla a chování
popisují [API kontrakt](API_CONTRACT.md) a
[stavy a přechody](SCREENS_AND_FLOWS.md); nevytvářejí další desktopový wireframe.

Úvodní adresa nyní vyžaduje přihlášení. Člen pokračuje na `/projects`, vyučující
na `/teaching`; jde o vstupní obrazovky s ověřenou identitou, obsah projektového
dashboardu dodá KAN-47. Původní komponenty ukázkového Kanbanu zůstávají v kódu,
ale nejsou veřejnou úvodní stránkou; API controllerů vyžaduje session.
Účty i serverové session používají PostgreSQL. Podrobnosti jsou
v [databázové dokumentaci](../../../database/AUTHENTICATION.md).
Autentizace nenahrazuje projektová oprávnění, která budou součástí projektových funkcí.

## Ověření a známá omezení

Spuštění a rozsah HTTP, databázových, frontendových a browser testů popisuje
[testovací dokumentace](../../TESTING.md). Výsledky jednotlivých běhů patří
do testovacích reportů a evidence Jira.

- Projektový dashboard s reálnými daty dodává KAN-47. Formulář založení
  projektu je zapojený do identity na `/projects/new`, zatím však neukládá
  projekt ani tým do API.
- Obnova hesla a potvrzení e-mailu nejsou součástí tohoto průchodu.
- Přijatý rozsah KAN-112 zahrnuje PC a emulaci mobilních/tabletových rozměrů.
  Chování fyzických zařízení a skutečné softwarové klávesnice není ověřené.
- První spuštění a navázání další funkce popisuje
  [integrační návod](INTEGRATION_GUIDE.md). Stav převzetí a CI se eviduje v Jiře.
