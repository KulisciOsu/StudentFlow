-- KAN-48: Přidání popisu projektu.
-- Popis je nepovinný, proto má výchozí prázdnou hodnotu.

BEGIN;

ALTER TABLE project
    ADD COLUMN description TEXT NOT NULL DEFAULT '';

ALTER TABLE project
    ADD CONSTRAINT chk_project_description_length
    CHECK (char_length(description) <= 1000);

COMMIT;