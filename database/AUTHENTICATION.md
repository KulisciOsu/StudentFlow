# KAN-109 / KAN-110 / KAN-116: účty a serverové session

## Schéma

V003 rozšiřuje existující tabulky; V001 a V002 se zpětně nepřepisují.

- `app_user.normalized_email` je generovaný sloupec `normalize_account_email(email)`
  s unikátním omezením. Ochrana funguje i pro přímé SQL a souběžné zápisy.
  Normalizace odstraní okolní Unicode whitespace a převede písmena na malá.
  Stejnou SQL funkci používá migrace, zápis i vyhledávání; zápis vrací skutečně
  uložený e-mail. Při migraci se ID a hashe účtů zachovají.
- Účet má právě jeden typ `STUDENT` (v UI Člen), nebo `TEACHER` (Vyučující).
  Jiné interní role se nezaměňují za typ účtu. Vedení projektu se stále
  odvozuje z konkrétního členství, nikoli z globální role `TEAM_LEADER`.
- Odložené constraint triggery kontrolují typ až při COMMIT. Lze proto
  atomicky založit účet a jeho roli; samotný účet bez typu ani dvojí typ
  nepotvrdí žádná cesta zápisu. Souběžné změny stejného účtu se serializují.
- Kódy systémových typů nelze přejmenovat a tím obejít kontrolu.
- `user_session` připravuje uložení SHA-256 hashe tokenu (32 bajtů), vlastníka,
  vytvoření, pevné expirace, trvalosti a případného zneplatnění. Neukládá token
  samotný. Omezení dovolí přesně 8 nebo 720 hodin podle volby trvalosti a
  nepovolí zneplatnění před vznikem session. Smazání účtu smaže jeho session.

Operace session implementuje KAN-116 přes `IUserSessionStore` a
`PostgresUserSessionStore`. Job pro úklid expirovaných session zatím zavedený není;
neplatné záznamy se při každém načtení odmítají.

## Upgrade existující databáze

V003 má vlastní transakci. Při kolizi normalizovaných e-mailů nebo chybějícím či
dvojím typu se zastaví s počtem problematických skupin/účtů. Změny se vrátí
zpět; migrace sama žádný účet nemaže ani nerozhoduje, který typ zachovat.
Před migrací použijte běžný zálohovací postup pro danou databázi.

Diagnostika duplicit:

```sql
WITH normalized AS (
    SELECT user_id, lower(btrim(email,
        U&'\0009\000A\000B\000C\000D\0020\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000')) AS email
    FROM app_user
)
SELECT email AS normalized_email,
       array_agg(user_id ORDER BY user_id) AS user_ids
FROM normalized
GROUP BY email
HAVING count(*) > 1;
```

Diagnostika typů:

```sql
SELECT u.user_id,
       array_agg(r.role_code) FILTER (WHERE r.role_code IN ('STUDENT', 'TEACHER')) AS account_types
FROM app_user u
LEFT JOIN user_role ur ON ur.user_id = u.user_id
LEFT JOIN app_role r ON r.role_id = ur.role_id
GROUP BY u.user_id
HAVING count(*) FILTER (WHERE r.role_code IN ('STUDENT', 'TEACHER')) <> 1;
```

Po výslovné opravě konfliktních dat lze V003 spustit znovu. Po úspěšné aplikaci
se již neopakuje; další změny patří do nové migrace. [Spuštění migrací](README.md).

## Datové operace

`IUserAccountStore` v Core vymezuje rozhraní. `PostgresUserAccountStore` v Data
implementuje parametrizované SQL přes Dapper a sdílený `NpgsqlDataSource`.

| Operace | Výsledek |
| --- | --- |
| `CreateAsync` | Účet se zvoleným typem v jediné transakci; duplicita má výsledek `EmailAlreadyRegistered`. Selhání vložení role vrátí celý zápis zpět. |
| `FindByEmailAsync` | Normalizované hledání účtu včetně údajů k ověření hesla; nenalezený účet vrací `null`. |
| `FindByIdAsync` | Identita, typ a příznak aktivity bez hashe. |
| `GetRoleCodesAsync` | Načtení uložených interních rolí; samo nedává projektové oprávnění. |
| `UpdatePasswordHashAsync` | Podmíněná aktualizace očekávaného hashe; starý požadavek nepřepíše souběžnou změnu hesla. |

Typ je v Core enum, do existující databáze se mapuje jako `STUDENT`/`TEACHER`.
Hashování a ověření hesla náleží backendu KAN-106/107. Data dostávají již hotový
hash. Hash není součástí veřejné identity a při JSON serializaci interních
objektů je vynechaný. Datová vrstva umožňuje načíst neaktivní účet, aby o odmítnutí
přihlášení se stejnou obecnou chybou rozhodla autentizační služba.

## Datové operace session

- `CreateAsync` zamkne vlastníka a znovu ověří aktivitu i očekávaný hash hesla.
  V jedné transakci provede případný rehash, vloží novou session a zneplatní
  platnou session předloženou stejnou cookie. Ostatní zařízení ponechá aktivní.
  Změněné heslo nebo deaktivovaný vlastník vrátí `null` bez zápisu; chyba
  vložení vrátí zpět také rehash a rotaci.
- `FindActiveAsync` načítá session podle 32bajtového hashe. Vyžaduje
  `revoked_at IS NULL`, `expires_at > now` a aktivního vlastníka. Vrací aktuální
  jméno, e-mail a jediný typ účtu; expiraci nemění.
- `RevokeAsync` je idempotentní. Čas zneplatnění neklesne před vznik session
  ani při posunu hodin zpět. Nenalezený token nezneplatní jiné session.

Čas dodává serverový `TimeProvider` v UTC. Expiraci počítá INSERT z téhož
okamžiku jako vznik, aby dodržel přesných 8/720 hodin i po převodu na
mikrosekundovou přesnost PostgreSQL. Raw token, heslo ani hash hesla nejsou
součástí veřejné odpovědi session.

Podklady: [PostgreSQL 17 constraint triggery](https://www.postgresql.org/docs/17/sql-createtrigger.html),
[NpgsqlDataSource a transakce](https://www.npgsql.org/doc/basic-usage.html),
[Dapper async API](https://github.com/DapperLib/Dapper/blob/main/Dapper/SqlMapper.Async.cs).
