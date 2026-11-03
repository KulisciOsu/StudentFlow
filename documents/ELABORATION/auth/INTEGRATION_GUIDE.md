# KAN-115: předání autentizace dalšímu vývojáři

Autentizace poskytuje ověřenou identitu a session pro nové stránky a API.
Účet je právě `STUDENT` (Člen), nebo `TEACHER` (Vyučující). Oprávnění ke
konkrétnímu projektu musí ověřit projektová funkce na serveru.

## Spuštění a první průchod

Použijte [DEVELOPMENT.md](../../DEVELOPMENT.md): kontrola nástrojů, instalace
závislostí, setup certifikátu, PostgreSQL 17 a migrace V001–V003. Konfigurace
zůstává v ignorovaném `.env` nebo prostředí serveru. Hesla a connection string
nepatří do `VITE_` proměnných, klientského kódu ani commitu.

1. Otevřete `https://localhost:60357/register` a založte člena s novým e-mailem.
2. Přihlaste se na navazující obrazovce. Ověřte jméno a jediný typ účtu v hlavičce.
3. Obnovte `/projects`; identita se načte pomocí `GET /api/auth/me`.
4. Přímé otevření `/teaching` členovi zobrazí odepřený přístup.
5. Odhlaste se a obnovte `/projects`; následuje přihlášení. Totéž zkuste
   s novým vyučujícím, jehož výchozí přehled je `/teaching`.

`/api/health` potvrzuje běh API. Teprve úspěšná registrace, login a `/me`
prověří i datovou vrstvu. Přehledy zatím obsahují zástupný text.

## Nový chráněný endpoint

Vzor [AccountSummaryController.cs](../../../examples/authentication/AccountSummaryController.cs)
čte `SessionIdentity` z `HttpContext.Features`, kam ji vloží autentizační
handler po ověření cookie, expirace a aktivního účtu v PostgreSQL.
Vrací pouze veřejné `AuthUserDto`: `id`, `displayName`, `email`, `accountType`.
Neužívá ID uživatele zaslané klientem pro určení vlastníka požadavku.

Vzor je mimo produkční projekt. Kompiluje se do HTTP testovacího projektu,
který jej načte přes `AddApplicationPart`; do běžícího produktu nepřidává
novou route. Pro vyzkoušení integrace v pracovní větvi:

1. Zkopírujte vzor do `src/StudentFlow.Api/Controllers/MyAccountController.cs`.
2. Třídu přejmenujte na `MyAccountController` a route na `api/my-account`.
   Tím se nová funkce neplete s původním testovaným vzorem.
3. Spusťte API. Anonymní `GET /api/my-account` vrátí JSON `401`; po loginu
   vrátí `200` a aktuálního uživatele. Přístup člena na
   `/api/my-account/teaching` skončí `403`, jeho session zůstane platná.

`[Authorize]` vyžaduje přihlášení. Varianta
`[Authorize(Roles = "TEACHER")]` povoluje pouze typ účtu vyučujícího.
Výchozí autorizační politika chrání také nové endpointy bez atributu;
`[AllowAnonymous]` použijte jen pro záměrně veřejné rozhraní.
Pouhé skrytí odkazu ve frontendu nechrání data.

Typ účtu není projektové oprávnění: registrace vyučujícího mu neposkytuje
přístup ke všem projektům. U projektového endpointu ověřte také vazbu
ověřeného uživatele na požadovaný projekt. Vedení týmu se odvozuje z členství;
nepřidávejte třetí typ účtu `TEAM_LEADER`.

## Nová stránka

Vzor [AccountSummaryPage.tsx](../../../src/studentflow.client/src/examples/AccountSummaryPage.tsx)
je kontrolovaný TypeScriptem a lintem, ale aplikace jej zatím nezobrazuje.
`useAuth()` vrací stav identity, `refresh`, `signIn` a `signOut`.
Data uživatele lze číst až ve stavu `authenticated` z `state.identity.user`.

Pro ověření spolu s předchozím controllerem změňte ve vzoru URL požadavku
na `/api/my-account`, importujte `AccountSummaryPage` do `App.tsx` a vložte
`<AccountSummaryPage key={user.id} />` místo současného zástupného `<section>`
ve větvi `route.path === home`. Zachovejte obal s `AuthHeader` a kontrolu typu
účtu `wrongType`. Úpravu proveďte až po zapojení endpointu.

Vzor načítá data přes relativní URL se stejným originem, ruší požadavek při
opuštění stránky a ignoruje pozdní odpověď. Při změně uživatele se díky
`key` zahodí předchozí data. `401` obnoví stav autentizace; `403` zobrazí
odepřený přístup bez odhlášení. Síťová chyba se nezobrazuje jako prázdný úspěšný
přehled. AuthProvider při obnovování, výpadku nebo odhlašování chráněný obsah
odmountuje; nová stránka musí zůstat uvnitř této ochrany.

Formulář KAN-86 je zapojený na `/projects/new` uvnitř stejné ochrany a pro
typ účtu `STUDENT` (přihlášený člen podle UC04). Otevírá se tlačítkem
„Založit projekt“ na `/projects`; návrat vede na tento přehled. V aplikaci
používá společný `AuthHeader` se skutečnou identitou a odhlášením.
Formulář zatím pouze validuje vstup, projekt ani tým do databáze neukládá.
Kontrola typu účtu ve frontendu nenahrazuje autorizaci budoucího zápisu v API.

Do `localStorage`, `sessionStorage`, URL ani historie neukládejte session,
heslo nebo kopii identity. Cookie přikládá prohlížeč; její hodnotu JavaScript
nečte. Domluvená životnost je pevná: 8 hodin, při „Zůstat přihlášen“ 30 dní.

## Zapisující požadavky a chyby

Pro `POST` lze použít `authPost` z `src/auth/client.ts`. Před zápisem získá
aktuální CSRF token přes `/api/auth/csrf` a přidá `X-CSRF-TOKEN`; token se
neukládá trvale. Nový backendový zápis musí token také ověřit, stejně jako
akce v `AuthController` pomocí `[ServiceFilter(typeof(CsrfValidationFilter))]`.
Filtr používá `IAntiforgery.ValidateRequestAsync` a sjednocuje chybu tokenu.
Samotný `[Authorize]` CSRF neověřuje. Vzorové GET endpointy data nemění.

`authPost` opakuje zápis pouze jednou po explicitním
`403` s kódem `CSRF_INVALID`. Jiný `403`, výpadek sítě ani neurčitý výsledek
zápisu neopakuje. Při `PUT`, `PATCH` či `DELETE` zachovejte stejná pravidla
v jejich klientovi; současný helper podporuje jen `POST`.

| Odpověď | Reakce nové funkce |
| --- | --- |
| `401 UNAUTHENTICATED` | Obnovit stav identity přes `refresh`; zobrazit přihlášení při neplatné session. |
| `403 FORBIDDEN` | Zobrazit chybějící oprávnění; neodhlašovat. |
| `403 CSRF_INVALID` | Získat nový token; helper má nejvýše jedno opakování. |
| `400`, `409` | Použít kontrakt konkrétní funkce; autentizační texty pro duplicitu e-mailu nepřebírat do projektů. |
| `429` | Respektovat `Retry-After`; nespouštět automatickou smyčku. |
| `503` nebo síťová chyba | Nabídnout obnovení; neprohlašovat zápis za úspěšný. |

Veřejný kontrakt, formát problémových odpovědí a bezpečnostní limity uvádí
[API_CONTRACT.md](API_CONTRACT.md). Projektová funkce potřebuje vlastní
validaci, autorizační pravidla a chybový kontrakt.

## Ověření a převzetí

Spuštění sad a oddělenou testovací databázi popisuje
[TESTING.md](../../TESTING.md). `HandoffExampleTests` přes skutečnou HTTP
pipeline a PostgreSQL ověřuje anonymní `401`, oba typy účtu, identitu ze
session navzdory podstrčenému `userId`, učitelské omezení `403` a odmítnutí
po logoutu. Klientský vzor prochází TypeScriptem a lintem; vlastní komponentové
a browser testy tohoto vzoru zatím přidané nejsou.

Pro převzetí jiným vývojářem zaznamenejte do KAN-115 revizi, prostředí a výsledek
prvního spuštění i předchozích průchodů. Vlastní kontrola autora nenahrazuje
toto převzetí. Výsledky jednotlivých běhů a převzetí evidujte v Jiře a testovacích reportech.

| Návaznost | Co ještě vyžaduje práci |
| --- | --- |
| KAN-115 / KAN-100 | Převzetí a spuštění jiným vývojářem. |
| KAN-47 | Projektový dashboard s reálnými daty, navázaný na `useAuth`; aktuálně pracuje jiný vývojář. |
| KAN-86 | Formulář je zapojený do identity; zbývá napojení na projektové API KAN-87/88 po koordinaci s autorem projektové části. |
| KAN-113 | Ověření workflow na GitHub Actions při prvním PR do `main`. |

Rozsah ověření KAN-112 zahrnuje PC a emulaci mobilních/tabletových viewportů.
Chování fyzických zařízení a skutečné softwarové klávesnice zůstává neověřené.

Obnova hesla, potvrzení e-mailu a projektová oprávnění nejsou součástí
aktuálního autentizačního průchodu. Jejich nepřítomnost neskrývejte za
zástupný úspěch nebo veřejné demo endpointy.

Podklady: [ASP.NET Core application parts](https://learn.microsoft.com/en-us/aspnet/core/mvc/advanced/app-parts?view=aspnetcore-10.0),
[React: useEffect a načítání dat](https://react.dev/reference/react/useEffect).
