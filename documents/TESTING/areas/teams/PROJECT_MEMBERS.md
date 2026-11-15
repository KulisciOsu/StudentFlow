# KAN-94: Testy členů projektu

Pravidla a API kontrakt popisuje [MEMBERS.md](../../../ELABORATION/projects/MEMBERS.md).
Obecné spouštění testů je v [TESTING.md](../../../TESTING.md).

## Automatické testy

| Úroveň | Soubor | Co ověřuje |
| :--- | :--- | :--- |
| Unit (Core) | `tests/StudentFlow.Core.Tests/ProjectMembers/ProjectMemberServiceTests.cs` | Všechna pravidla služby: kdo smí číst a měnit, e-mail, typ a aktivita účtu, duplicita, odebrání vedoucího a nečlena. |
| Unit (Core) | `tests/StudentFlow.Core.Tests/Authentication/AccountEmailRulesTests.cs` | Sdílené pravidlo platného e-mailu (registrace i přidání člena). |
| Databáze | `tests/StudentFlow.Data.IntegrationTests/ProjectMemberMigrationTests.cs` | Migrace: `added_by` s `SET NULL`, trigger jen pro účty `STUDENT` včetně odložené kontroly, jeden vedoucí. |
| Databáze | `tests/StudentFlow.Data.IntegrationTests/PostgresProjectMemberStoreTests.cs` | Dapper úložiště na PostgreSQL 17: seznam a řazení, přidání a odebrání jedním podmíněným příkazem, duplicita, vyučující, úkoly odebraného člena zůstanou. |
| HTTP | `tests/StudentFlow.Api.IntegrationTests/ProjectMembersApiTests.cs` | Každý stavový kód z kontraktu včetně chybějícího CSRF, `404` pro nečlena, `403` pro člena u POST a DELETE, vedoucí nemůže odebrat sám sebe, limit a typ těla. |
| Vitest | `services/projects/projectMembersApi.test.ts`, `services/http/csrfRequest.test.ts`, `auth/client.test.ts` | Zpracování odpovědí, mapování kódů na české zprávy, CSRF token, `authPost` beze změny chování. |
| Vitest | `hooks/projects/useProjectMembers.test.ts`, `utils/projects/memberInitials.test.ts` | Načítání seznamu, 401, opakování; iniciály avataru. |
| Vitest | `components/projects/settings/ProjectSettingsPage.test.tsx`, `components/projects/members/ProjectMembers.test.tsx`, `AddMemberForm.test.tsx` | Stavy stránky podle role, potvrzení odebrání v řádku, vypnutá tlačítka během požadavku, chyby v `role="alert"`, jedna zpráva o poslední akci. |
| E2E | `src/studentflow.client/tests/project-members.spec.ts` | 6 scénářů v Chromium, Firefox a WebKit: přidání a odebrání na mobilu, tabletu a desktopu, chyby u vyučujícího a neexistujícího účtu, běžný člen (i obejitím stránky) a uživatel mimo projekt. |

E2E používá seedový účet vedoucího `e2e-vedouci@studentflow.test` v ukázkovém
projektu 1, který nastavuje `scripts/e2e-api.mjs`. Testy přidávají a odebírají jen
čerstvě registrované účty a nekontrolují celkový počet členů, protože ukázkový
projekt současně používají testy nástěnky.

## Ruční scénáře

Příprava: vedoucí ukázkového projektu podle
[Ruční vyzkoušení](../../../ELABORATION/projects/MEMBERS.md#ruční-vyzkoušení),
druhý registrovaný účet typu Člen a jeden účet typu Vyučující. Stránka:
`/projects/1/settings`.

| ID | Kroky | Očekávaný výsledek |
| :--- | :--- | :--- |
| M1 | Vedoucí otevře stránku. | Nadpis „Členové týmu“ s počtem, název týmu, vedoucí s „(vy)“ a odznakem „Vedoucí“, bez tlačítka „Odebrat“ u vedoucího. |
| M2 | Zadá e-mail registrovaného studenta a klikne „Přidat do týmu“. | Tlačítko během odesílání ukazuje „Přidáváme…“, pole se vyprázdní, zpráva „… je nyní členem týmu.“, nový řádek „Člen“. |
| M3 | Zadá stejný e-mail znovu. | „Tento uživatel už je členem týmu.“, e-mail zůstane v poli, žádný druhý řádek. |
| M4 | Zadá e-mail účtu Vyučující. | „Do týmu lze přidat jen účet studenta.“ |
| M5 | Zadá neregistrovaný e-mail, potom prázdné pole. | „Účet s tímto e-mailem v StudentFlow neexistuje.“, potom „Zadejte e-mail studenta.“ |
| M6 | U člena klikne „Odebrat“, potom „Zrušit“. | Zobrazí se „Opravdu odebrat?“ s fokusem na „Zrušit“. Po zrušení zůstane člen beze změny. |
| M7 | Klikne „Odebrat“ a potvrdí „Odebrat“. | „… už není členem týmu.“, řádek zmizí, starší zpráva o přidání už není vidět. |
| M8 | Odebraný člen obnoví nastavení nebo nástěnku v jiném prohlížeči. | Přístup ztratí ihned (pravidlo 6). Jeho úkoly na nástěnce zůstanou. |
| M9 | Běžný člen otevře stránku. | „Nemáte oprávnění“, žádný seznam ani formulář. |
| M10 | Uživatel mimo projekt otevře stránku a adresu neexistujícího projektu, např. `/projects/999/settings`. | V obou případech stejná zpráva „Projekt neexistuje nebo k němu nemáte přístup.“ |
| M11 | Šířka 390 px, otevřené potvrzení odebrání. | Bez vodorovného posouvání, potvrzení se zalomí pod jméno. |
| M12 | Ovládání jen klávesnicí (Tab, Enter). | Všechna tlačítka jsou dosažitelná, Enter po otevření potvrzení nikoho neodebere. |

## Známé problémy při lokálním E2E na Windows

Nejsou způsobené touto funkcí; v CI na Linuxu se neprojevují.

- Celá sada ve třech prohlížečích s výchozím počtem workerů notebook přetíží a
  testy padají na časové limity. Spouštějte `--workers=1` (jako CI) nebo jeden
  prohlížeč přes `--project=chromium`.
- WebKit na Windows hlásí cookie session se `SameSite=None` místo `Lax`, proto tam
  selhávají kontroly cookie v `session.spec.ts`.
- Přihlašovací formulář se může sám vymazat, když `AuthProvider` znovu ověří
  přihlášení (událost `visibilitychange` nebo zpráva z jiné karty) a na okamžik
  zobrazí „Ověřujeme přihlášení…“. WebKit tuto událost někdy pošle těsně po
  vyplnění e-mailu, takže test přihlášení náhodně selže s „Zadejte platnou
  e-mailovou adresu.“. Uživatel může stejný efekt zažít po přepnutí karty.
  Poznámka pro autora autentizace (Kryštof).
