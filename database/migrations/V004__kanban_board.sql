-- KAN-98: projekty, sloupce a úkoly nástěnky.
BEGIN;

-- 1. Projekty. Každý projekt patří jednomu týmu.
CREATE TABLE project (
    project_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name VARCHAR(200) NOT NULL CHECK (btrim(name) <> ''),
    team_name VARCHAR(120) NOT NULL CHECK (btrim(team_name) <> ''),
    due_date DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 2. Sloupce nástěnky. Každý projekt má vlastní sloupce v daném pořadí.
CREATE TABLE board_column (
    column_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    project_id INTEGER NOT NULL REFERENCES project(project_id) ON DELETE CASCADE,
    title VARCHAR(80) NOT NULL CHECK (btrim(title) <> ''),
    position INTEGER NOT NULL CHECK (position >= 0),
    is_done BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT uq_board_column_position UNIQUE (project_id, position) DEFERRABLE INITIALLY DEFERRED,
    CONSTRAINT uq_board_column_project UNIQUE (column_id, project_id)
);

-- 3. Úkoly. Složený cizí klíč na sloupec zaručuje, že sloupec patří do stejného projektu jako úkol.
--    Pozice ve sloupci jsou jedinečné; kontrola je odložená na konec transakce, aby šlo úkoly přeskládat.
CREATE TABLE task (
    task_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    project_id INTEGER NOT NULL REFERENCES project(project_id) ON DELETE CASCADE,
    column_id INTEGER NOT NULL,
    position INTEGER NOT NULL CHECK (position >= 0),
    title VARCHAR(200) NOT NULL CHECK (btrim(title) <> ''),
    priority VARCHAR(10) CHECK (priority IN ('low', 'medium', 'high')),
    assignee_id BIGINT NOT NULL REFERENCES app_user(user_id) ON DELETE RESTRICT,
    due_date DATE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT fk_task_column FOREIGN KEY (column_id, project_id) REFERENCES board_column(column_id, project_id),
    CONSTRAINT uq_task_position UNIQUE (column_id, position) DEFERRABLE INITIALLY DEFERRED
);

CREATE INDEX ix_board_column_project ON board_column(project_id);
CREATE INDEX ix_task_project ON task(project_id);
CREATE INDEX ix_task_assignee ON task(assignee_id);

COMMIT;
