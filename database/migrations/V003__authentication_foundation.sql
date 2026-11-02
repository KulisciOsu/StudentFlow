-- KAN-109: existující účty se zachovají; nejednoznačná data vyžadují opravu.
BEGIN;

LOCK TABLE app_user, app_role, user_role IN SHARE ROW EXCLUSIVE MODE;

-- Stejné okolní Unicode whitespace jako String.Trim v .NET.
CREATE FUNCTION trim_account_text(value text) RETURNS text
LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS $$
    SELECT btrim(value,
        U&'\0009\000A\000B\000C\000D\0020\0085\00A0\1680\2000\2001\2002\2003\2004\2005\2006\2007\2008\2009\200A\2028\2029\202F\205F\3000')
$$;

-- Databáze je autoritou pro převod písmen při migraci, zápisu i vyhledávání.
CREATE FUNCTION normalize_account_email(value text) RETURNS text
LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS $$
    SELECT lower(trim_account_text(value))
$$;

DO $$
DECLARE
    collision_count integer;
    invalid_account_count integer;
BEGIN
    SELECT count(*) INTO collision_count
    FROM (
        SELECT normalize_account_email(email)
        FROM app_user
        GROUP BY normalize_account_email(email) HAVING count(*) > 1
    ) collisions;
    IF collision_count > 0 THEN
        RAISE EXCEPTION 'KAN-109: % skupin účtů má shodný normalizovaný e-mail.', collision_count
            USING ERRCODE = '23505',
                  HINT = 'Nejprve vyřešte kolize podle diagnostických dotazů v database/AUTHENTICATION.md.';
    END IF;

    SELECT count(*) INTO invalid_account_count
    FROM app_user u
    WHERE (SELECT count(*) FROM user_role ur JOIN app_role r USING (role_id)
           WHERE ur.user_id = u.user_id AND r.role_code IN ('STUDENT', 'TEACHER')) <> 1;
    IF invalid_account_count > 0 THEN
        RAISE EXCEPTION 'KAN-109: % účtů nemá právě jeden typ STUDENT nebo TEACHER.', invalid_account_count
            USING ERRCODE = '23514',
                  HINT = 'Typy účtů opravte výslovně podle zadání; migrace je sama nevybírá.';
    END IF;
END $$;

UPDATE app_user SET email = normalize_account_email(email);
ALTER TABLE app_user
    ADD COLUMN normalized_email varchar(320)
        GENERATED ALWAYS AS (normalize_account_email(email)) STORED NOT NULL,
    ADD CONSTRAINT app_user_normalized_email_key UNIQUE (normalized_email),
    ADD CONSTRAINT app_user_email_not_blank CHECK (char_length(normalized_email) >= 3),
    ADD CONSTRAINT app_user_display_name_not_blank CHECK (trim_account_text(display_name) <> ''),
    ADD CONSTRAINT app_user_password_hash_not_blank CHECK (trim_account_text(password_hash) <> '');

-- Kontrola až při COMMIT dovoluje vložit účet a jeho typ v jedné transakci.
-- Zámek rodiče serializuje změny rolí stejného uživatele, včetně souběžných zápisů.
CREATE FUNCTION enforce_single_account_type() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    affected_ids bigint[];
    affected_id bigint;
    type_count integer;
BEGIN
    IF TG_TABLE_NAME = 'app_user' THEN
        affected_ids := ARRAY[NEW.user_id];
    ELSIF TG_OP = 'INSERT' THEN
        affected_ids := ARRAY[NEW.user_id];
    ELSIF TG_OP = 'DELETE' THEN
        affected_ids := ARRAY[OLD.user_id];
    ELSE
        affected_ids := ARRAY[OLD.user_id, NEW.user_id];
    END IF;
    FOR affected_id IN SELECT DISTINCT unnest(affected_ids) ORDER BY 1 LOOP
        -- I nezměněná hodnota vytváří verzi řádku: také REPEATABLE READ musí
        -- při souběhu skončit serializační chybou, nikoli přehlédnout cizí typ.
        UPDATE app_user SET updated_at = updated_at WHERE user_id = affected_id;
        IF FOUND THEN
            SELECT count(*) INTO type_count
            FROM user_role ur JOIN app_role r USING (role_id)
            WHERE ur.user_id = affected_id AND r.role_code IN ('STUDENT', 'TEACHER');
            IF type_count <> 1 THEN
                RAISE EXCEPTION 'Účet musí mít právě jeden typ STUDENT nebo TEACHER.'
                    USING ERRCODE = '23514', CONSTRAINT = 'user_account_type_exactly_one';
            END IF;
        END IF;
    END LOOP;
    RETURN NULL;
END $$;

CREATE CONSTRAINT TRIGGER app_user_account_type_required
AFTER INSERT ON app_user DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION enforce_single_account_type();

CREATE CONSTRAINT TRIGGER user_role_account_type_required
AFTER INSERT OR UPDATE OR DELETE ON user_role DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION enforce_single_account_type();

-- Přejmenování systémových kódů nesmí obejít kontrolu typu účtu.
CREATE FUNCTION protect_account_type_codes() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF OLD.role_code IS DISTINCT FROM NEW.role_code
       AND (OLD.role_code IN ('STUDENT', 'TEACHER') OR NEW.role_code IN ('STUDENT', 'TEACHER')) THEN
        RAISE EXCEPTION 'Kódy typů účtu STUDENT a TEACHER nelze měnit.'
            USING ERRCODE = '23514', CONSTRAINT = 'account_type_codes_immutable';
    END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER app_role_protect_account_type_codes
BEFORE UPDATE OF role_code ON app_role
FOR EACH ROW EXECUTE FUNCTION protect_account_type_codes();

CREATE TABLE user_session (
    session_id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id bigint NOT NULL REFERENCES app_user(user_id) ON DELETE CASCADE,
    token_hash bytea NOT NULL UNIQUE CHECK (octet_length(token_hash) = 32),
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    persistent boolean NOT NULL DEFAULT false,
    revoked_at timestamptz,
    CONSTRAINT user_session_absolute_lifetime CHECK (
        expires_at = created_at + CASE WHEN persistent THEN interval '720 hours' ELSE interval '8 hours' END
    ),
    CONSTRAINT user_session_revocation_time CHECK (revoked_at IS NULL OR revoked_at >= created_at)
);
CREATE INDEX user_session_user_id_idx ON user_session(user_id);

COMMIT;
