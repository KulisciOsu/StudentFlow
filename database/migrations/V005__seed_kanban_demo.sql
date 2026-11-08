-- KAN-98: ukázková nástěnka pro vývoj. Řešitelé jsou členové bez platného hesla, nelze se za ně přihlásit.
BEGIN;

DO $$
DECLARE
    v_project_id INTEGER;
    v_todo INTEGER;
    v_progress INTEGER;
    v_review INTEGER;
    v_done INTEGER;
BEGIN
    INSERT INTO app_user (email, display_name, password_hash)
    VALUES
        ('petra.svobodova@studentflow.local', 'Petra Svobodová', 'not-set'),
        ('jan.novak@studentflow.local', 'Jan Novák', 'not-set'),
        ('tomas.dvorak@studentflow.local', 'Tomáš Dvořák', 'not-set'),
        ('lucie.cerna@studentflow.local', 'Lucie Černá', 'not-set'),
        ('martin.kucera@studentflow.local', 'Martin Kučera', 'not-set')
    ON CONFLICT DO NOTHING;

    -- Každý účet musí mít právě jeden typ (V003); ukázkoví řešitelé jsou členové.
    INSERT INTO user_role (user_id, role_id)
    SELECT app_user.user_id, app_role.role_id
    FROM app_user
    JOIN app_role ON app_role.role_code = 'STUDENT'
    WHERE app_user.email LIKE '%@studentflow.local'
      AND NOT EXISTS (SELECT 1 FROM user_role WHERE user_role.user_id = app_user.user_id)
    ORDER BY app_user.user_id;

    INSERT INTO project (name, team_name, due_date)
    VALUES ('Rezervační systém knihovny', 'Tým Sova', DATE '2027-01-15')
    RETURNING project_id INTO v_project_id;

    INSERT INTO board_column (project_id, title, position, is_done)
    VALUES (v_project_id, 'K řešení', 0, FALSE)
    RETURNING column_id INTO v_todo;

    INSERT INTO board_column (project_id, title, position, is_done)
    VALUES (v_project_id, 'Rozpracováno', 1, FALSE)
    RETURNING column_id INTO v_progress;

    INSERT INTO board_column (project_id, title, position, is_done)
    VALUES (v_project_id, 'Ke kontrole', 2, FALSE)
    RETURNING column_id INTO v_review;

    INSERT INTO board_column (project_id, title, position, is_done)
    VALUES (v_project_id, 'Hotovo', 3, TRUE)
    RETURNING column_id INTO v_done;

    INSERT INTO task (project_id, column_id, position, title, priority, assignee_id, due_date)
    SELECT v_project_id, seed.column_id, seed.position, seed.title, seed.priority, app_user.user_id, seed.due_date
    FROM (
        VALUES
            (v_todo, 0, 'Testovací scénáře přihlášení', 'medium', 'lucie.cerna@studentflow.local', DATE '2026-11-12'),
            (v_todo, 1, 'Formulář rezervace', 'high', 'tomas.dvorak@studentflow.local', DATE '2026-11-20'),
            (v_todo, 2, 'Export výpůjček do CSV', 'low', 'martin.kucera@studentflow.local', DATE '2026-12-04'),
            (v_progress, 0, 'Návrh databázového modelu', 'high', 'jan.novak@studentflow.local', DATE '2026-10-22'),
            (v_progress, 1, 'API pro výpůjčky', 'medium', 'martin.kucera@studentflow.local', DATE '2026-11-05'),
            (v_progress, 2, 'Wireframy obrazovek', 'medium', 'petra.svobodova@studentflow.local', DATE '2026-11-09'),
            (v_review, 0, 'Diagram případů užití', 'medium', 'lucie.cerna@studentflow.local', DATE '2026-11-06'),
            (v_done, 0, 'Vize projektu', NULL, 'petra.svobodova@studentflow.local', DATE '2026-10-10'),
            (v_done, 1, 'Popis problému', NULL, 'jan.novak@studentflow.local', DATE '2026-10-12'),
            (v_done, 2, 'Risk-list', NULL, 'lucie.cerna@studentflow.local', DATE '2026-10-15')
    ) AS seed (column_id, position, title, priority, email, due_date)
    JOIN app_user ON app_user.email = seed.email
    ORDER BY seed.column_id, seed.position;
END $$;

COMMIT;
