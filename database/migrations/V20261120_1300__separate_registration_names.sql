-- KAN-132: nové účty ukládají jméno a příjmení odděleně.
-- Původní display_name zůstává pouze pro starší účty, bez odhadování jeho částí.
BEGIN;

ALTER TABLE app_user
    ALTER COLUMN display_name DROP NOT NULL,
    DROP CONSTRAINT app_user_display_name_not_blank,
    ADD COLUMN first_name varchar(160),
    ADD COLUMN last_name varchar(160),
    ADD CONSTRAINT app_user_name_parts_complete CHECK (
        (first_name IS NULL AND last_name IS NULL AND display_name IS NOT NULL
            AND trim_account_text(display_name) <> '')
        OR
        (first_name IS NOT NULL AND last_name IS NOT NULL AND display_name IS NULL
            AND length(first_name) BETWEEN 1 AND 160 AND length(last_name) BETWEEN 1 AND 160
            AND first_name !~ U&'[\0009-\000D\0020\0085\00A0\1680\2000-\200A\2028\2029\202F\205F\3000\FEFF]'
            AND last_name !~ U&'[\0009-\000D\0020\0085\00A0\1680\2000-\200A\2028\2029\202F\205F\3000\FEFF]')
    );

COMMENT ON COLUMN app_user.display_name IS 'Původní nerozdělené jméno starších účtů; nové účty vyplňují first_name a last_name.';

COMMIT;
