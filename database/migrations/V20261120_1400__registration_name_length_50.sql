-- KAN-132: technický limit samostatného jména a příjmení je 1–50 znaků.
-- Původní nerozdělená jména zůstávají beze změny; delší části se nikdy neořezávají.
BEGIN;

LOCK TABLE app_user IN ACCESS EXCLUSIVE MODE;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM app_user WHERE length(first_name) > 50 OR length(last_name) > 50) THEN
        RAISE EXCEPTION 'KAN-132: některé existující části jména přesahují 50 znaků.'
            USING ERRCODE = '23514',
                  HINT = 'Před migrací výslovně opravte dotčené testovací údaje; automatické zkrácení se neprovádí.';
    END IF;
END $$;

ALTER TABLE app_user
    DROP CONSTRAINT app_user_name_parts_complete,
    ALTER COLUMN first_name TYPE varchar(50),
    ALTER COLUMN last_name TYPE varchar(50),
    ADD CONSTRAINT app_user_name_parts_complete CHECK (
        (first_name IS NULL AND last_name IS NULL AND display_name IS NOT NULL
            AND trim_account_text(display_name) <> '')
        OR
        (first_name IS NOT NULL AND last_name IS NOT NULL AND display_name IS NULL
            AND length(first_name) BETWEEN 1 AND 50 AND length(last_name) BETWEEN 1 AND 50
            AND first_name !~ U&'[\0009-\000D\0020\0085\00A0\1680\2000-\200A\2028\2029\202F\205F\3000\FEFF]'
            AND last_name !~ U&'[\0009-\000D\0020\0085\00A0\1680\2000-\200A\2028\2029\202F\205F\3000\FEFF]')
    );

COMMIT;
