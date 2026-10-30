INSERT INTO app_role (role_id, role_code, role_name)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'STUDENT', 'Student'),
    (2, 'TEAM_LEADER', 'Vedoucí týmu'),
    (3, 'TEACHER', 'Učitel'),
    (4, 'ADMIN', 'Administrátor')
ON CONFLICT DO NOTHING;