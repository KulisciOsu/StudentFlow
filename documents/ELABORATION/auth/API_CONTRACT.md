# KAN-114: API kontrakt autentizace

**Schválený kontrakt, verze 0.4.0.** JSON struktury, příklady a HTTP odpovědi
jsou v [auth.openapi.yaml](auth.openapi.yaml). Zde jsou význam a pravidla,
která samotné schéma nezachytí. Změna smlouvy musí změnit oba soubory.

Implementované jsou všechny zde uvedené endpointy. Aktuální rozsah a validační
bod popisuje [stav implementace](README.md).

## Endpointy

| Metoda a adresa | Přístup | Úspěch | Výsledek |
| :--- | :--- | :--- | :--- |
| `GET /api/auth/csrf` | Veřejný | `200` | `{ token, headerName: "X-CSRF-TOKEN" }` a podpůrná antiforgery cookie. |
| `POST /api/auth/register` | Veřejný + CSRF | `201` | `{ user }`; účet a právě jeden typ v jediné transakci, bez přihlášení. |
| `POST /api/auth/login` | Veřejný + CSRF | `200` | `{ user, session }` a cookie s novou serverovou session. |
| `GET /api/auth/me` | Platná session | `200` | `{ user, session }`; identita ověřená serverem. |
| `POST /api/auth/logout` | CSRF, i bez platné session | `204` | Zneplatnění aktuální session a odstranění cookie; bez těla. |

Všechny odpovědi včetně chyb mají `Cache-Control: no-store`. API nepřesměrovává
na HTML přihlášení. JSON používá `camelCase`, čas UTC ve formátu RFC 3339 a ID
uživatele jako řetězec číslic: existující PostgreSQL `BIGINT` může přesáhnout
přesnost JavaScript `number`. Vstupy POST používají `application/json` kromě
logoutu bez těla. Neznámá pole se odmítají jako `400 VALIDATION_FAILED`.

## Vstupy a validace

| Pole | Registrace | Login | Pravidlo |
| :--- | :--- | :--- | :--- |
| `displayName` | Povinné | — | Po oříznutí 1–160 Unicode znaků; samotné bílé znaky nestačí. Bez unikátnosti. |
| `email` | Povinné | Povinné | Po oříznutí 3–320 znaků, platná e-mailová adresa, převod na malá písmena. |
| `password` | Povinné | Povinné | 15–128 Unicode znaků; bez ořezání, změny velikosti či Unicode normalizace. |
| `accountType` | Povinné | — | Jediný řetězec `STUDENT` (Člen) nebo `TEACHER` (Vyučující). Pole, kombinace, prázdná hodnota a `null` se odmítají. |
| `rememberMe` | — | Volitelné | Boolean, výchozí `false`; neurčuje typ účtu ani oprávnění. |

Nejprve se normalizují jméno a e-mail, pak validují délky. OpenAPI popisuje
normalizovanou podobu. Heslo se měří v Unicode code pointech; frontend a backend
musí délku počítat shodně, nikoli UTF-16 jednotkami. Není požadována kombinace
velkých písmen, číslic a symbolů. Hesla nesmějí být v URL, logu ani odpovědi API.

JSON registrace i loginu má limit **16 KiB**, včetně požadavků bez `Content-Length`.
Překročení vrací `413 PAYLOAD_TOO_LARGE` před hashováním a zápisem.
Heslo se ukládá pomocí ASP.NET Core Identity `PasswordHasher` ve formátu V3:
PBKDF2-HMAC-SHA512, 220 000 iterací, náhodná sůl pro každý hash. Parametry
jsou součástí uloženého formátu a umožní pozdější ověření a rehash při loginu.

`accountType` se mapuje na právě jednu z existujících rolí `STUDENT` a `TEACHER`.
`STUDENT` se v rozhraní zobrazuje jako „Člen“. Zadaný typ `ADMIN`, `TEAM_LEADER`,
neznámá hodnota, pole typů nebo staré pole `accountTypes` znamená `400`.
Registrace nepřijímá ID, aktivitu účtu ani informace o projektech.
Duplicitní normalizovaný e-mail odmítá i databáze. Souběžné registrace nesmějí
vytvořit dva účty nebo účet bez zvoleného typu.

Pravidlo jednoho typu platí i při uložení a načítání účtů, nikoli pouze ve
formuláři. V navazující migraci KAN-109 se musí odhalit existující účty s žádným
nebo oběma typy a před použitím přihlášení výslovně opravit; typ se nevybírá
automaticky podle pořadí rolí. Pokud API zjistí takto nekonzistentní účet,
vrátí `500 INTERNAL_ERROR` bez citlivých detailů a nevydá úspěšnou identitu.

## Identita a předání dashboardu KAN-47

```json
{
  "user": {
    "id": "42",
    "displayName": "Jan Novák",
    "email": "jan.novak@example.test",
    "accountType": "STUDENT"
  },
  "session": {
    "expiresAt": "2026-11-02T20:00:00Z",
    "persistent": false
  }
}
```

- Při startu/refreshi frontend zavolá `/me`. Do výsledku nezobrazuje chráněný
  obsah. `200` ustaví sdílený stav identity pro dashboard i další stránky.
- `user.accountType` obsahuje právě jednu hodnotu `STUDENT` nebo `TEACHER`.
  Další interní role se zde neposílají; chybějící nebo dvojí typ není platná identita.
- Člen pokračuje na `/projects`, vyučující na `/teaching`. Přepínač mezi typy
  účtu se nenabízí. Typ potřebný pro stránku se ověřuje i při přímém otevření URL.
- Dashboard sdílí stav poskytovatele autentizace, nevytváří vlastní identitu
  v `localStorage` a neodvozuje přihlášení z přítomnosti cookie.
- Projektová API kontrolují členství nebo přidělený přístup na serveru při
  každém požadavku. `TEACHER` nedává přístup ke všem projektům. Vedení týmu
  vzniká z konkrétního členství, nikoli z globální role nebo typu účtu.
- Autentizace nevrací projekty ani jejich oprávnění. Obsah dashboardu dodává
  KAN-47 a navazující tasky; zde se vymezují cesty a předání identity.

## Session a cookies

- Cookie `__Host-StudentFlow.Session`: `Secure`, `HttpOnly`, `SameSite=Lax`,
  `Path=/`, bez `Domain`. Také vývoj proto používá HTTPS.
- Cookie nese neprůhledný náhodný token. Vlastník, role a platnost se ověřují
  na serveru. V databázi se ukládá hash tokenu, nikoli token samotný.
- Bez `rememberMe`: absolutní expirace za 8 hodin od loginu, cookie bez
  `Expires`/`Max-Age`. S `rememberMe`: 30 dní a cookie s odpovídajícím `Expires`
  a `Max-Age=2592000`.
- `/me`, aktivita ani nový CSRF token platnost neprodlužují. Rozhoduje serverový
  čas; `expiresAt` ve frontendu je pouze pomůcka pro UI.
- Každý úspěšný login vytvoří novou session. Případnou platnou session
  předloženou stejnou cookie zneplatní, ostatní zařízení ponechá přihlášená.
- Token je 32 náhodných bajtů z kryptografického generátoru, do cookie kódovaný
  jako Base64url. Databáze dostane pouze jeho SHA-256 hash.
- Vytvoření session, případný rehash a rotace předchozí session jsou jedna
  transakce. Před zápisem se pod zámkem znovu ověří aktivita a nezměněný hash
  hesla; souběžná změna hesla nebo deaktivace nevytvoří novou session.
- Každý chráněný požadavek ověří existenci, nezneplatnění a expiraci session
  i aktivitu vlastníka. Neplatnost znamená `401 UNAUTHENTICATED`.
- Logout zneplatní session před odpovědí `204`. I pro chybějící nebo již
  neplatnou session vrátí `204` a odstraní cookie stejným názvem a cestou.
  Při chybě databáze vrátí `503`, nikoli falešný úspěch.

## CSRF a chyby

1. Před prvním POST frontend načte `/csrf`. Server vydá antiforgery cookie
   `__Host-StudentFlow.Csrf` (`Secure`, `HttpOnly`, `SameSite=Lax`, `Path=/`,
   bez `Domain`) a odpovídající request token v JSON.
2. Token se posílá hlavičkou `X-CSRF-TOKEN` při registraci, loginu, logoutu
   a později dalších zápisech. Veřejný endpoint není výjimkou. Neplatný token
   vrací `403 CSRF_INVALID` před změnou dat.
3. Po loginu, logoutu nebo zjištěném `401` se token zahodí. Pro další zápis
   se načte nový, odpovídající současné identitě.
4. Jen při explicitním `403 CSRF_INVALID` lze jednou obnovit token a zopakovat
   požadavek. Síťová chyba, timeout ani `5xx` neopravňují k automatickému
   opakování zápisu: jeho výsledek může být neznámý.
5. Registrace a login mají nastavitelné omezení frekvence; vyčerpání vrací
   `429` a `Retry-After` v sekundách. Registrace má výchozí limit 20 požadavků
   za 60 minut na IP v pevném okně, bez fronty. Konfigurace:
   `Auth:RegistrationRateLimit:PermitLimit` a `Auth:RegistrationRateLimit:WindowMinutes`.
   Limit je v paměti jedné instance; více instancí vyžaduje společný limit na proxy.
   Aplikace sama nedůvěřuje hlavičce `X-Forwarded-For`. Cizí neúspěšné pokusy
   nevedou k trvalému zablokování účtu. Login má samostatný limit 30 požadavků
   za 15 minut na IP; nastavení je pod `Auth:LoginRateLimit:PermitLimit`
   a `Auth:LoginRateLimit:WindowMinutes`.

Chyby mají `application/problem+json`; klient rozhoduje podle `status` a `code`,
nikoli textu. Stejný tvar používají i chyby frameworku, CSRF a rate limiteru.

```json
{
  "type": "about:blank",
  "title": "Neplatné vstupní údaje",
  "status": 400,
  "detail": "Opravte označená pole.",
  "code": "VALIDATION_FAILED",
  "traceId": "request-correlation-id",
  "errors": { "password": ["Heslo musí mít 15 až 128 znaků."] }
}
```

| HTTP | Kód | Chování klienta |
| :--- | :--- | :--- |
| `400` | `VALIDATION_FAILED` | Chyby u polí a souhrn; také chybný JSON či typ pole. |
| `401` login | `INVALID_CREDENTIALS` | Jedna zpráva pro neexistující/neaktivní účet i špatné heslo. |
| `401` chráněné API | `UNAUTHENTICATED` | Skrýt soukromá data, zrušit identitu a přejít na login. |
| `403` | `CSRF_INVALID` | Obnovit token, nejvýše jednou opakovat. |
| `403` projektové API | `FORBIDDEN` | Zobrazit nedostatek oprávnění; ponechat přihlášení. Mimo pět auth endpointů. |
| `409` registrace | `EMAIL_ALREADY_REGISTERED` | Označit e-mail a nabídnout login. Výslovná informace o duplicitě je navržené chování k validaci. |
| `413` registrace / login | `PAYLOAD_TOO_LARGE` | Požadavek přesahuje 16 KiB; neopakovat beze změny. |
| `415` | `UNSUPPORTED_MEDIA_TYPE` | Chyba klienta, neopakovat beze změny. |
| `429` | `RATE_LIMITED` | Doba čekání podle `Retry-After`; poté akce uživatele, ne automatický POST. |
| `500` / `503` | `INTERNAL_ERROR` / `SERVICE_UNAVAILABLE` | Obecná zpráva, bez detailů databáze či výjimky. |

Síťová chyba a timeout nemají HTTP status a nezaměňují se za `401`. Při nejisté
registraci se nehlásí úspěch. Při nejistém loginu lze bezpečně ověřit `/me`;
logout má samostatný chybový stav v návrhu obrazovek.

## Zapojení autentizace

ASP.NET Core používá vlastní autentizační scheme `StudentFlowSession`, která
pro každý požadavek s cookie načte platnou session a aktuálního aktivního
vlastníka z PostgreSQL. Role v claims obsahuje jediný typ účtu. Výchozí
autorizační politika vyžaduje přihlášení; veřejné jsou healthcheck, CSRF,
registrace, login a logout. Neplatná session vrací `401` v JSON, bez HTML redirectu.
Chyba databáze je `503`, nikoli `401`. Autentizace sama nenahrazuje budoucí
kontrolu projektového členství.

Frontend sdílí identitu přes `AuthProvider`. Při startu, návratu do viditelné
karty, změně přihlášení v jiné kartě a při dosažení oznámené expirace ověří `/me`.
Při ověřování a odhlašování skrývá chráněný obsah. Ruší předchozí požadavky
a používá generaci požadavku, aby starší odpověď neobnovila odhlášenou identitu.
Při chybě logoutu obsah zůstane skrytý až do potvrzeného opakování. V URL,
`localStorage`, `sessionStorage` ani `history.state` neukládá identitu, token či heslo.

## Podklady pro implementaci

- [KAN-114](https://kulisciosu.atlassian.net/browse/KAN-114),
  [KAN-46](https://kulisciosu.atlassian.net/browse/KAN-46), [návrh rozhodnutí](README.md).
- [OpenAPI 3.1.1](https://spec.openapis.org/oas/v3.1.1.html): formát smlouvy a cookie security scheme.
- [Microsoft: CSRF v ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0): cookie autentizace potřebuje CSRF ochranu; token může klient posílat v hlavičce.
- [OWASP: ukládání hesel](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html): pracovní faktor PBKDF2-HMAC-SHA512.
- [Microsoft: PasswordHasher](https://source.dot.net/Microsoft.Extensions.Identity.Core/PasswordHasher.cs.html): formát Identity V3 a náhodná sůl.
- [Microsoft: rate limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit?view=aspnetcore-10.0): pevné okno a metadata `RetryAfter`.
- [Microsoft: AuthenticationHandler](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.authenticationhandler-1?view=aspnetcore-10.0): vlastní scheme a oddělení ověření, challenge a forbidden.

Názvy cookie, DTO, endpointů a politika obnovy tokenu jsou návrh projektu,
nikoli předepsaná výchozí konfigurace ASP.NET Core.
