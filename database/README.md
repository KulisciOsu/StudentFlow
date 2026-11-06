
# Databáze StudentFlow

Projekt používá PostgreSQL 17 provozovaný v Dockeru. Kompletní prostředí API
a frontendu popisuje [vývojový návod](../documents/DEVELOPMENT.md).

## Požadavky
- Docker Desktop
- pgAdmin 4 (volitelné)

## 1. Konfigurace

Společný setup z kořene projektu vytvoří `.env` s náhodným lokálním heslem:

```bash
node scripts/dev.mjs setup
```

Pro samotnou databázi bez Node/.NET lze zkopírovat `.env.example` do `.env`
a vyplnit neprázdné `POSTGRES_PASSWORD`. Soubor `.env` není verzovaný v Gitu.
U existujícího svazku použijte původní heslo; změna `.env` heslo v DB nezmění.

## 2. Spuštění PostgreSQL

V kořenové složce projektu spusťte:

```powershell
docker compose up -d --wait
```

Databáze bude dostupná na:
- Host: 127.0.0.1
- Port: 5433
- Database: studentflow
- Username: studentflow
- Password: hodnota z `.env`

## 3. Vytvoření tabulek a rolí

Na novém datovém svazku se SQL soubory z `database/migrations/` automaticky
provedou při prvním startu kontejneru. Následující ruční postup je určen pouze
pro starší svazek, kde ještě příslušné migrace nebyly provedeny. Na databázi
inicializované automaticky znovu nespouštějte V001.

V003 přidává normalizované unikátní e-maily, právě jeden typ účtu a schéma
session. [Pravidla, diagnostika a datové operace](AUTHENTICATION.md).

Migrace zakládají číselník rolí. Vývojové účty člena a vyučujícího se vytvářejí až při
startu API v prostředí `Development`, také na již inicializované databázi.
[Přihlašovací údaje a vypnutí seedu](../documents/DEVELOPMENT.md#testovací-účty-pro-lokální-vývoj).

Příkazy spusťte v terminálu PowerShell
z kořenové složky projektu.

Nejprve nastavte kódování:

```powershell
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
```

Potom spusťte migrace v uvedeném pořadí:

```powershell
Get-Content .\database\migrations\V001__initial_schema.sql -Raw -Encoding UTF8 | docker exec -i studentflow-db psql -v ON_ERROR_STOP=1 -U studentflow -d studentflow --single-transaction
```

```powershell
Get-Content .\database\migrations\V002__seed_roles.sql -Raw -Encoding UTF8 | docker exec -i studentflow-db psql -v ON_ERROR_STOP=1 -U studentflow -d studentflow --single-transaction
```

Migraci V001 spouštějte pouze na nové databázi.
Opakované spuštění skončí chybou,
protože tabulky již existují.

Po V001 a V002 se na existující databázi provede V003. Má vlastní transakci,
proto se zde nepřidává `--single-transaction`:

```powershell
Get-Content .\database\migrations\V003__authentication_foundation.sql -Raw -Encoding UTF8 | docker exec -i studentflow-db psql -v ON_ERROR_STOP=1 -U studentflow -d studentflow
```

V Bash z kořene projektu:

```bash
docker compose exec -T postgres psql -v ON_ERROR_STOP=1 -U studentflow -d studentflow < database/migrations/V003__authentication_foundation.sql
```

Úspěšně aplikované migrace se neopakují. Při odmítnutí V003 kvůli starým datům
nejprve vyřešte hlášené konflikty podle diagnostiky, potom V003 opakujte.

## 4. Zastavení databáze

```powershell
docker compose stop
```

Data zůstávají uložená v Docker volume.
