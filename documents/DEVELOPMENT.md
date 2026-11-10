# KAN-117: společné vývojové prostředí

## Požadavky

- Node.js 22 (alespoň 22.13 podle [požadavků ESLintu 10](https://eslint.org/docs/latest/use/getting-started#prerequisites); verze řady je v `.nvmrc`).
- .NET 10 SDK; `global.json` dovoluje stabilní aktualizace v řadě 10.0.
- Docker s Compose v2 pro PostgreSQL 17.

Příkazy fungují z kořene repozitáře v Bash i PowerShellu. Všechny cesty mají
stejnou velikost písmen i na Linuxu.

## První spuštění

Z kořene nejprve ověřte dostupné nástroje. Kontrola nic neinstaluje, nemění
konfiguraci a nevypisuje hesla:

```bash
node scripts/check-env.mjs
```

Při chybě skončí návratovým kódem 1 a uvede chybějící požadavek. Ověřuje i běh
Docker enginu, ale nenahrazuje kontrolu databáze a migrací.

Pokud kontrola hlásí odmítnutí přístupu k Docker enginu, zobrazte konkrétní
chybu pomocí `docker version`. V Linuxu/WSL může po přidání do skupiny
`docker` zůstat terminál se starými oprávněními. Porovnejte aktivní skupiny
(`id -nG`) se zapsaným členstvím (`getent group docker`). Pokud už ve skupině
zapsaní jste, načtěte ji v novém podshellu a kontrolu zopakujte:

```bash
newgrp docker
node scripts/check-env.mjs
```

Nová relace a aplikace spuštěné z ní použijí aktualizované skupiny. Postup
popisuje také [Docker: obnovení členství ve skupině](https://docs.docker.com/engine/install/linux-postinstall/#manage-docker-as-a-non-root-user).

```bash
npm --prefix src/studentflow.client ci
node scripts/dev.mjs setup
docker compose up -d --wait
node scripts/dev.mjs
```

Setup vytvoří ignorovaný `.env` s náhodným heslem databáze a exportuje lokální
vývojový certifikát do ignorované `.certs/`. Existující `.env` nepřepisuje.
Vzor konfigurace je [.env.example](../.env.example). API přebírá heslo ze stejného
souboru jako Compose; connection string se předává pouze prostředím serveru,
nikoli jako `VITE_` proměnná nebo součást klientského balíčku.

Při existující databázi nastavte v `.env` její původní heslo. Změna proměnné
PostgreSQL nemění heslo uživatele uvnitř existujícího datového svazku.

Při prvním startu nového datového svazku PostgreSQL automaticky provede SQL
soubory v `database/migrations/` v pořadí názvů. Existující svazek se automaticky
nemění; další migrace se provádějí podle [databázového návodu](../database/README.md).

| Služba | Adresa |
| --- | --- |
| Aplikace | `https://localhost:60357` |
| API přímo | `https://localhost:7082` |
| Kontrola běhu API přes aplikaci | `https://localhost:60357/api/health` |
| Registrace člena nebo vyučujícího | `https://localhost:60357/register` |
| Přihlášení | `https://localhost:60357/login` |
| PostgreSQL | `127.0.0.1:5433`, databáze a uživatel `studentflow` |

Používejte host `localhost` i stejné schéma HTTPS. Frontend posílá relativní
požadavky `/api/...`, které Vite proxy předá API. Prohlížeč tak používá jeden
origin připravený pro zabezpečené cookies. Kontrola `/api/health` ověřuje běh
API; dostupnost databáze ověřuje Compose healthcheck, nikoli tento endpoint.

Registrace a login již vyžadují funkční databázi po migraci V003. Úvodní
adresa přesměruje na přihlášení; po loginu člen pokračuje na dashboard `/projects`
a vyučující na `/teaching`. Dashboard zatím používá ukázková data projektů
a úkolů. Ukázkové API sloupců a úkolů je také chráněné session.

Pro důvěru vývojovému certifikátu lze podle platformy použít:

```bash
dotnet dev-certs https --trust
```

V Linuxu/WSL může být nutné nastavit důvěru také v používaném prohlížeči či
Windows. Setup důvěru systému automaticky nemění. `secure: false` se týká pouze
lokální Vite proxy; produkční konfigurace ověření TLS neoslabuje.

Společný příkaz vyčká na připravené API a poté spustí frontend, aby první
načtení neselhalo během sestavování backendu. `Ctrl+C` zastaví API i Vite.
Databáze zůstává spuštěná; zastavíte ji
bez smazání dat:

```bash
docker compose stop
```

Samotné API lze spustit pomocí `node scripts/dev.mjs api`. Pro frontend
samostatně slouží `npm --prefix src/studentflow.client run dev`.

## Testovací účty pro lokální vývoj

Vývojový seed umí při startu API založit účet člena (`STUDENT`) a vyučujícího
(`TEACHER`). Přihlašovací údaje nastavte ve vlastním lokálním `.env`, který Git
ignoruje. Jejich aktuální hodnoty najdete pouze v tomto souboru; projekt
neobsahuje výchozí e-maily ani heslo. Názvy proměnných uvádí také `.env.example`:

| Proměnná | Význam |
| --- | --- |
| `DevelopmentSeed__MemberEmail` | Vlastní e-mail účtu člena. |
| `DevelopmentSeed__TeacherEmail` | Vlastní, odlišný e-mail účtu vyučujícího. |
| `DevelopmentSeed__LeaderEmail` | Volitelný, odlišný e-mail účtu vedoucího (typ `STUDENT`). |
| `DevelopmentSeed__Password` | Heslo pro všechny nově vytvářené účty, 15 až 128 znaků Unicode; mezery jsou jeho součástí. |
| `DevelopmentSeed__Enabled` | Nastavte `true` až po vyplnění všech přihlašovacích údajů. |

Nejdříve musí běžet databáze s provedenými migracemi V001–V003. Potom spusťte
`node scripts/dev.mjs`, který předá `.env` aplikaci. Při samostatném spuštění
přes `dotnet run` předejte stejné hodnoty jako proměnné prostředí.

Pokud databáze obsahuje ukázkovou nástěnku (migrace nástěnky a ukázkových dat), seed přidá člena
a vedoucího do ukázkového projektu „Rezervační systém knihovny“. Člen převezme
úkoly Jana Nováka, vedoucí úkoly Petry Svobodové a vedení projektu. Vyučující
vidí všechny nástěnky jen pro čtení.

Seed je standardně vypnutý. V prostředí `Production` ani `Testing` se účty
nezakládají, ani při ručním povolení konfigurace. Pro vypnutí při vývoji nastavte
`DevelopmentSeed__Enabled=false`. Chybějící nebo neplatné údaje při povoleném
seedu zastaví start API před vytvořením účtů.

Účty vytváří API přes stejné hashování a datové operace jako registrace.
Do databáze ukládá hash s vlastní náhodnou solí pro každý účet. Opakovaný start
nevytváří duplicity. Existující účet podle e-mailu zůstane zachovaný včetně
hesla, jména, typu a aktivity. Změna konfigurace nepřepisuje ani nepřejmenovává
existující účty; doplní pouze chybějící účty podle nových e-mailů.

Odstranění údajů ze současných souborů je neodstraňuje z historie Gitu.
Přepnutí prostředí na `Production` již založené testovací účty neodstraní.

## Vlastní vývojová PostgreSQL bez Dockeru

Lze použít také vlastní PostgreSQL **17** a samostatnou vývojovou databázi.
Vytvořte databázi a uživatele, který vlastní její schéma. Na prázdné databázi
proveďte V001, V002 a V003 v tomto pořadí. Například v Bash s klientem `psql`
(heslo zadáte do jeho výzvy):

```bash
psql -h 127.0.0.1 -p 5433 -U studentflow -d studentflow -v ON_ERROR_STOP=1 --single-transaction -f database/migrations/V001__initial_schema.sql
psql -h 127.0.0.1 -p 5433 -U studentflow -d studentflow -v ON_ERROR_STOP=1 --single-transaction -f database/migrations/V002__seed_roles.sql
psql -h 127.0.0.1 -p 5433 -U studentflow -d studentflow -v ON_ERROR_STOP=1 -f database/migrations/V003__authentication_foundation.sql
```

V003 má vlastní transakci. Na existující databázi neopakujte již provedené
migrace; postup a diagnostiku uvádí [databázový návod](../database/README.md).

Před spuštěním nastavte connection string v prostředí serveru. V Bash:

```bash
export ConnectionStrings__DefaultConnection='Host=127.0.0.1;Port=5433;Database=studentflow;Username=studentflow;Password=your-local-password'
node scripts/check-env.mjs --external-db
```

V PowerShellu použijte `$env:ConnectionStrings__DefaultConnection = '...'`.
Nahraďte uvedené hodnoty vlastní konfigurací. Potom proveďte instalaci
závislostí, `node scripts/dev.mjs setup` a `node scripts/dev.mjs`; krok
`docker compose up` vynechte. Proměnná prostředí má přednost před `.env`.
Kontrola `--external-db` ověřuje nastavení proměnné, nikoli připojení nebo
verzi serveru. Ty ověřte v PostgreSQL a registračním průchodem.
Pro testy použijte oddělený server podle [TESTING.md](TESTING.md).

## Kontroly

```bash
dotnet build src/StudentFlow.Api/StudentFlow.Api.csproj -c Release
npm --prefix src/studentflow.client run lint
npm --prefix src/studentflow.client run audit:dependencies
npm --prefix src/studentflow.client run build
```

Build frontendu nevyžaduje certifikát ani `.env`. End-to-end testy používají
stejné HTTPS prostředí; postup je v [TESTING.md](TESTING.md).

`audit:dependencies` zahrnuje produkční i vývojové závislosti a skončí chybou
při nálezu střední nebo vyšší závažnosti. V `package.json` je dočasný override
`minimatch: 11.0.1`, který opravuje
[GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249).
Současný ESLint a TypeScript ESLint požadují řadu 10, proto nestačí běžná
aktualizace v jejich povoleném rozsahu. Řada 11 odstraňuje některá rozhraní;
kompatibilita aktuálního lintu byla ověřena spuštěním nad všemi jeho soubory
i odmítnutím chybného TS/TSX vstupu. Při aktualizaci lintovacích nástrojů override
znovu posuďte; odstraňte jej, až jejich vlastní závislosti zvolí opravenou
verzi a projdou lint, build, testy i úplný audit. Podrobnosti: KAN-123.

Použití autentizace v další funkci a kontrolní postup pro předání popisuje
[integrační návod KAN-115](ELABORATION/auth/INTEGRATION_GUIDE.md).

Podklady: [dotnet dev-certs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs),
[Vite server options](https://vite.dev/config/server-options).
