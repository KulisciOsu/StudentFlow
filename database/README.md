
# Databáze StudentFlow

Projekt používá PostgreSQL 17 provozovaný v Dockeru.

## Požadavky
- Docker Desktop
- pgAdmin 4 (volitelné)

## 1. Konfigurace

V kořenové složce projektu vytvořte soubor `.env`:

```dotenv
POSTGRES_PASSWORD=StudentFlow
```

Soubor `.env` není verzovaný v Gitu.

## 2. Spuštění PostgreSQL

V kořenové složce projektu spusťte:

```powershell
docker compose up -d
```

Databáze bude dostupná na:
- Host: 127.0.0.1
- Port: 5433
- Database: studentflow
- Username: studentflow
- Password: hodnota z `.env`

## 3. Vytvoření tabulek a rolí

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

## 4. Zastavení databáze

```powershell
docker compose stop
```

Data zůstávají uložená v Docker volume.
