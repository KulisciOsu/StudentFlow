# KAN-50: Členové projektu

Vedoucí týmu spravuje členy na stránce **Nastavení projektu**
(`/projects/{projectId}/settings`): vidí seznam členů, přidá registrovaného
studenta podle e-mailu a po potvrzení v řádku odebere běžného člena. Na stránku
vede odkaz „Nastavení projektu“ na nástěnce, který vidí jen vedoucí (API projektu
vrací `isLeader`). Zpět vede drobečková navigace „Moje projekty / Nástěnka“.

Části epiku: wireframe KAN-90, frontend KAN-91, backend KAN-92, databáze KAN-93,
testy KAN-94. Testovací scénáře jsou v
[TESTING/areas/teams](../../TESTING/areas/teams/PROJECT_MEMBERS.md).

## Pravidla

| # | Pravidlo | Kde se vynucuje |
| :--- | :--- | :--- |
| 1 | Projekt má právě jednoho vedoucího (`is_leader`), ostatní jsou běžní členové. | Nejvýše jeden: unikátní index `ux_project_member_leader` (V006). Alespoň jeden: aplikace, protože vedoucího nelze odebrat a předání vedení zatím neexistuje. |
| 2 | Seznam členů smí číst každý člen projektu včetně vedoucího. | API při každém požadavku |
| 3 | Přidávat a odebírat smí jen vedoucí. | Jediný podmíněný SQL příkaz: ověření vedoucího a změna proběhnou najednou, bez mezery pro souběh. |
| 4 | Přidává se e-mailem existujícího aktivního účtu typu `STUDENT`. E-maily se porovnávají normalizovaně (`normalize_account_email`). Neaktivní účet se hlásí stejně jako neexistující. | Aplikace a constraint trigger `project_member_student_required` |
| 5 | Vedoucí nemůže odebrat sám sebe. Odebrání nečlena vrací „nenalezeno“. | SQL podmínka `NOT is_leader` |
| 6 | Odebraný člen ztrácí přístup okamžitě. Členství se ověřuje při každém požadavku, frontendu se nevěří. | API |
| 7 | Úkoly, komentáře a další příspěvky odebraného člena zůstávají beze změny. | Odebrání maže jen řádek `project_member`. |
| 8 | Nečlen dostane stejnou odpověď jako u neexistujícího projektu (`404`), aby se neprozradily cizí projekty (riziko R3). Člen bez oprávnění k akci dostane `403`. | API |
| 9 | Vedoucí může zjistit, zda je e-mail v StudentFlow registrovaný. | Vědomě přijato, viz [Známá omezení](#známá-omezení). |

## Databáze

Tabulka `project_member` vznikla v migraci Honzy (V006, KAN-83) a je sdílená
s dashboardem: `project_id`, `user_id`, `is_leader`, `joined_at`, primární klíč
`(project_id, user_id)` a nejvýše jeden vedoucí na projekt.

Migrace `V20261114_2156__project_member_management.sql` (KAN-93) ji doplňuje,
bez úpravy V006:

- `added_by`: kdo člena přidal; při smazání jeho účtu se nastaví `NULL`
  (`fk_project_member_added_by`, `ON DELETE SET NULL`).
- Odložený constraint trigger `project_member_student_required`: členem může být
  jen účet s rolí `STUDENT`. Porušení hlásí `23514` s názvem
  `project_member_student_only`, které API převede na `422 ACCOUNT_NOT_STUDENT`.
  Kontrola proběhne až při potvrzení transakce, takže lze v jedné transakci
  založit účet i členství.

## API

Všechny endpointy vyžadují přihlášení (session cookie) a vracejí
`Cache-Control: no-store`. ID uživatele je řetězec číslic, stejně jako v
[API autentizace](../auth/API_CONTRACT.md). Zapisující požadavky vyžadují
hlavičku `X-CSRF-TOKEN` z `GET /api/auth/csrf`.

| Metoda a adresa | Kdo smí | Úspěch | Výsledek |
| :--- | :--- | :--- | :--- |
| `GET /api/projects/{projectId}/members` | Člen projektu | `200` | `{ teamName, members: [{ userId, displayName, email, isLeader, joinedAt }] }`, vedoucí první, ostatní podle jména (česká abeceda). |
| `POST /api/projects/{projectId}/members` | Vedoucí + CSRF | `201` | Tělo `{ email }`, odpověď `{ member }`. JSON nejvýše 16 KiB, neznámá pole se odmítají. |
| `DELETE /api/projects/{projectId}/members/{userId}` | Vedoucí + CSRF | `204` | Bez těla. |

Chyby mají tvar ProblemDetails s polem `code`:

| Status | `code` | Kdy |
| :--- | :--- | :--- |
| `400` | `VALIDATION_FAILED` | Prázdný nebo neplatný e-mail, neznámé pole v JSON. Pole `errors.email` obsahuje zprávu. |
| `401` | `UNAUTHENTICATED` | Chybí platná session. |
| `403` | `CSRF_INVALID` | Chybí nebo nesedí CSRF token (POST, DELETE). |
| `403` | `PROJECT_LEADER_REQUIRED` | Běžný člen zkouší přidat nebo odebrat. |
| `404` | `PROJECT_NOT_FOUND` | Projekt neexistuje, nebo volající není jeho členem (pravidlo 8). |
| `404` | `MEMBER_NOT_FOUND` | Odebíraný uživatel není členem projektu. |
| `409` | `MEMBER_ALREADY_EXISTS` | Účet už je členem; duplicitní řádek nevznikne. |
| `409` | `LEADER_CANNOT_BE_REMOVED` | Pokus odebrat vedoucího. |
| `413` | `PAYLOAD_TOO_LARGE` | Tělo POST větší než 16 KiB. |
| `415` | `UNSUPPORTED_MEDIA_TYPE` | Tělo POST není `application/json`. |
| `422` | `ACCOUNT_NOT_FOUND` | Žádný aktivní účet s tímto e-mailem. |
| `422` | `ACCOUNT_NOT_STUDENT` | Účet je typu `TEACHER`. Vyučující dostanou přístup přes UC09, který není součástí tohoto epiku. |

## Frontend

| Soubor | Účel |
| :--- | :--- |
| `components/projects/settings/ProjectSettingsPage.tsx` | Stránka, načítání a chyby. Běžnému členovi ukáže „Nemáte oprávnění“, nečlenovi chybu jako u neexistujícího projektu. |
| `components/projects/members/ProjectMembers.tsx` | Sekce „Členové týmu“: seznam, „Odebrat“ s potvrzením „Opravdu odebrat?“ v řádku a jedna zpráva o výsledku poslední akce. |
| `components/projects/members/AddMemberForm.tsx` | Formulář „Přidat člena“. |
| `hooks/projects/useProjectMembers.ts`, `useMemberRemoval.ts` | Načtení seznamu a logika odebírání. |
| `services/projects/projectMembersApi.ts` | Volání API a české zprávy podle `code`. |
| `services/http/csrfRequest.ts` | Sdílený pomocník pro zapisující požadavky s CSRF tokenem. Používá ho i `authPost` z autentizace. |

Skrytí formuláře a tlačítek je jen pohodlí pro uživatele, oprávnění vždy ověřuje
server. Po každé změně i po chybě, která mohla znamenat změnu na serveru, se
seznam znovu načte.

## Známá omezení

- **Zjištění registrovaného e-mailu (pravidlo 9).** Odpovědi `ACCOUNT_NOT_FOUND`
  a `ACCOUNT_NOT_STUDENT` prozradí vedoucímu, zda e-mail patří účtu a jakého
  typu. Wireframe vyžaduje přidání registrovaného účtu, proto je to zatím přijato.
  Případná náhrada je pozvánka e-mailem se stejnou odpovědí pro všechny adresy.
- **Smazání účtu vedoucího.** Cizí klíč `user_id` ve V006 má `ON DELETE CASCADE`,
  takže smazání účtu vedoucího smaže i jeho vedení a projekt zůstane bez
  vedoucího. Aplikace zatím mazání účtů nenabízí; až vznikne, musí tento případ
  řešit, například předáním vedení.
- **Předání vedení** zatím neexistuje, vedoucí nemůže projekt opustit.
- **Jeden účet = jeden typ.** Vyučující nemůže být členem týmu. Pokud tým přejde
  na role podle projektu (jeden účet může být v různých projektech členem,
  vedoucím i vyučujícím), je třeba odstranit trigger
  `project_member_student_required`, pravidlo `AccountNotStudent` v
  `ProjectMemberService`, jeho testy a zprávu na frontendu.

## Poznámky pro navazující epiky

**Epik 3, zakládání projektu (Vladimír):**
- Zakladatele vložte do `project_member` s `is_leader = TRUE` **ve stejné
  transakci** jako projekt (UC04: žádný projekt bez týmu). PR #17 to už dělá.
- `POST /api/projects` musí předem odmítnout účet typu `TEACHER` srozumitelnou
  chybou `4xx`. Jinak transakci zastaví trigger `project_member_student_required`
  a API vrátí `500`.

**Epik 5, nástěnka (Marek):**
- Seznam členů projektu, například pro výběr řešitele, načítejte přes
  `GET /api/projects/{projectId}/members`. Smí ho číst každý člen.
- Navržené společné pravidlo: `404` = projekt neexistuje **nebo** volající není
  členem, `403` = člen bez oprávnění k akci. Nástěnka zatím vrací nečlenovi `403`.
- `PUT .../tasks/{id}/position` zatím neposílá CSRF token. Lze použít
  `csrfRequest` ze `services/http/csrfRequest.ts`.

**Vývojový seed:** vývojový účet vyučujícího nedávejte do domény
`@studentflow.local`. Migrace `V20261110_1832__seed_kanban_demo_members.sql`
přidává do ukázkového projektu všechny účty v této doméně a trigger by ji na
databázi s naší migrací zastavil.

## Ruční vyzkoušení

Nejjednodušší je vývojový seed: s vyplněným `DevelopmentSeed__LeaderEmail` se
tento účet při startu API stane vedoucím ukázkového projektu
([vývojový návod](../../DEVELOPMENT.md#testovací-účty-pro-lokální-vývoj)).

Vlastní registrovaný účet typu Člen (`STUDENT`) udělá vedoucím ukázkového
projektu (na nové databázi má ID 1) tento SQL příkaz. Je **jen pro lokální
vývoj**. Otevřete `docker exec -it studentflow-db psql -U studentflow -d studentflow`,
nahraďte oba výskyty e-mailu svým a vložte:

```sql
BEGIN;
UPDATE project_member SET is_leader = FALSE
WHERE project_id = 1 AND is_leader
  AND EXISTS (SELECT 1 FROM app_user WHERE normalized_email = normalize_account_email('vas@email.cz'));
INSERT INTO project_member (project_id, user_id, is_leader)
SELECT 1, user_id, TRUE FROM app_user WHERE normalized_email = normalize_account_email('vas@email.cz')
ON CONFLICT (project_id, user_id) DO UPDATE SET is_leader = TRUE;
COMMIT;
```

Dosavadní vedoucí zůstane běžným členem. S neexistujícím e-mailem příkaz nic
nezmění. Potom otevřete `https://localhost:60357/projects/1/settings`. Ukázkové
členy (Jan Novák, Lucie Černá a další) přidává migrace
`V20261110_1832__seed_kanban_demo_members.sql`; na novém datovém svazku proběhne
automaticky.
