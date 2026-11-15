-- KAN-93: rozšíření členství v projektu pro přidávání a odebírání členů (KAN-50).
-- Vyžaduje V006__dashboard_membership.sql; tabulku project_member z KAN-83 jen rozšiřuje, existující sloupce nemění.
BEGIN;

-- 1. Kdo člena přidal. Chybí u vedoucího zapsaného při založení projektu a u řádků vzniklých před touto migrací.
--    Smazání účtu toho, kdo člena přidal, členství nezruší, jen údaj vymaže.
ALTER TABLE project_member
    ADD COLUMN added_by BIGINT
        CONSTRAINT fk_project_member_added_by REFERENCES app_user(user_id) ON DELETE SET NULL;

-- 2. Členem projektu smí být jen účet typu STUDENT; vyučující získávají přístup jinak (UC09).
--    Kontrola až při COMMIT, stejně jako typ účtu ve V003, dovoluje vložit účet, jeho typ i členství v jedné transakci.
--    Hlídá zápisy do project_member; změnu typu účtu u existujícího člena aplikace nenabízí.
CREATE FUNCTION enforce_project_member_student() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM user_role ur JOIN app_role r USING (role_id)
        WHERE ur.user_id = NEW.user_id AND r.role_code = 'STUDENT'
    ) THEN
        RAISE EXCEPTION 'Členem projektu může být jen účet typu STUDENT.'
            USING ERRCODE = '23514', CONSTRAINT = 'project_member_student_only';
    END IF;
    RETURN NULL;
END $$;

CREATE CONSTRAINT TRIGGER project_member_student_required
AFTER INSERT OR UPDATE OF user_id ON project_member DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION enforce_project_member_student();

COMMIT;
