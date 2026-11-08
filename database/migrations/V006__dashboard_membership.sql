
-- KAN-83: Členství uživatelů v projektech
-- Vyžaduje V004__kanban_board.sql

BEGIN;

CREATE TABLE project_member (
    project_id INTEGER NOT NULL
        REFERENCES project(project_id) ON DELETE CASCADE,
    user_id BIGINT NOT NULL
        REFERENCES app_user(user_id) ON DELETE CASCADE,
    is_leader BOOLEAN NOT NULL DEFAULT FALSE,
    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT pk_project_member
        PRIMARY KEY (project_id, user_id)
);

-- V jednom projektu může být nejvýše jeden vedoucí.
CREATE UNIQUE INDEX ux_project_member_leader
    ON project_member (project_id)
    WHERE is_leader = TRUE;

-- Rychlé vyhledávání projektů uživatele.
CREATE INDEX ix_project_member_user
    ON project_member (user_id);

COMMIT;
