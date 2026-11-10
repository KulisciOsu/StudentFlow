-- KAN-122: ukázkoví řešitelé jsou členy demo projektu, Petra Svobodová je vedoucí.
-- Vyžaduje V005__seed_kanban_demo.sql a V006__dashboard_membership.sql.
BEGIN;

INSERT INTO project_member (project_id, user_id, is_leader)
SELECT project.project_id, app_user.user_id, app_user.email = 'petra.svobodova@studentflow.local'
FROM project
JOIN app_user ON app_user.email LIKE '%@studentflow.local'
WHERE project.name = 'Rezervační systém knihovny'
ORDER BY app_user.user_id
ON CONFLICT DO NOTHING;

COMMIT;
