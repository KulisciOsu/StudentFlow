-- KAN-132: potvrzení verze zásad při nové registraci; starším účtům se souhlas nevymýšlí.
BEGIN;

ALTER TABLE app_user
    ADD COLUMN privacy_policy_version varchar(32),
    ADD COLUMN privacy_accepted_at timestamptz,
    ADD CONSTRAINT app_user_privacy_acceptance_complete CHECK (
        (privacy_policy_version IS NULL AND privacy_accepted_at IS NULL)
        OR (privacy_policy_version IS NOT NULL AND privacy_accepted_at IS NOT NULL
            AND length(btrim(privacy_policy_version)) BETWEEN 1 AND 32)
    );

COMMIT;
