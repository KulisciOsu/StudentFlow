
using Dapper;
using Npgsql;
using StudentFlow.Core.Dashboard;

namespace StudentFlow.Data.Dashboard;

/// <summary>
/// Načítá projekty, členy, statistiky a úkoly dashboardu z PostgreSQL.
/// </summary>
public sealed class PostgresDashboardRepository(
    NpgsqlDataSource dataSource) : IDashboardRepository
{
    public async Task<DashboardData> GetDashboardAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        const string projectsSql = """
            SELECT
                p.project_id AS Id,
                p.name AS Name,
                p.team_name AS Team,

                CASE
                    WHEN pm.is_leader THEN 'Vedoucí'
                    ELSE 'Člen'
                END AS Role,

                COALESCE(to_char(p.due_date, 'DD. MM. YYYY'), '') AS Milestone,

                (
                    SELECT COUNT(*)
                    FROM project_member members
                    WHERE members.project_id = p.project_id
                ) AS MembersCount,

                (
                    SELECT COUNT(*)
                    FROM task t
                    WHERE t.project_id = p.project_id
                ) AS Total,

                (
                    SELECT COUNT(*)
                    FROM task t
                    JOIN board_column c
                        ON c.column_id = t.column_id
                    WHERE t.project_id = p.project_id
                      AND c.is_done = TRUE
                ) AS Completed,

                (
                    SELECT COUNT(*)
                    FROM task t
                    JOIN board_column c
                        ON c.column_id = t.column_id
                    WHERE t.project_id = p.project_id
                      AND t.assignee_id = @UserId
                      AND c.is_done = FALSE
                ) AS OpenTasks,

                (
                    SELECT COUNT(*)
                    FROM task t
                    JOIN board_column c
                        ON c.column_id = t.column_id
                    WHERE t.project_id = p.project_id
                      AND t.assignee_id = @UserId
                      AND t.due_date < CURRENT_DATE
                      AND c.is_done = FALSE
                ) AS OverdueTasks

            FROM project p
            JOIN project_member pm
                ON pm.project_id = p.project_id
            WHERE pm.user_id = @UserId
            ORDER BY p.name;
            """;

        var projects = (await connection.QueryAsync<DashboardProjectData>(
            new CommandDefinition(
                projectsSql,
                new { UserId = userId },
                cancellationToken: cancellationToken))).AsList();

        const string membersSql = """
            SELECT
                pm.project_id AS ProjectId,
                u.display_name AS DisplayName
            FROM project_member pm
            JOIN app_user u
                ON u.user_id = pm.user_id
            WHERE pm.project_id IN (
                SELECT project_id
                FROM project_member
                WHERE user_id = @UserId
            )
            ORDER BY pm.project_id, u.display_name;
            """;

        var members = await connection.QueryAsync<DashboardMemberRow>(
            new CommandDefinition(
                membersSql,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        var membersByProject = members
            .GroupBy(m => m.ProjectId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(m => m.DisplayName).ToList());

        foreach (var project in projects)
        {
            if (membersByProject.TryGetValue(project.Id, out var names))
            {
                project.Members = names;
            }
        }

        const string tasksSql = """
            SELECT
                t.task_id AS Id,
                t.title AS Name,
                p.name AS Project,
                COALESCE(to_char(t.due_date, 'DD. MM.'), '') AS Deadline,

                CASE t.priority
                    WHEN 'high' THEN 'Vysoká'
                    WHEN 'medium' THEN 'Střední'
                    WHEN 'low' THEN 'Nízká'
                    ELSE 'Neuvedena'
                END AS Priority,

                c.title AS Status,
                (t.due_date < CURRENT_DATE AND NOT c.is_done) AS Overdue,
                c.is_done AS Completed

            FROM task t
            JOIN project p
                ON p.project_id = t.project_id
            JOIN project_member pm
                ON pm.project_id = p.project_id
            JOIN board_column c
                ON c.column_id = t.column_id
            WHERE t.assignee_id = @UserId
              AND pm.user_id = @UserId
            ORDER BY c.is_done, t.due_date NULLS LAST, t.task_id;
            """;

        var tasks = await connection.QueryAsync<DashboardTaskData>(
            new CommandDefinition(
                tasksSql,
                new { UserId = userId },
                cancellationToken: cancellationToken));

        return new DashboardData
        {
            Projects = projects,
            Tasks = tasks.AsList()
        };
    }

    private sealed class DashboardMemberRow
    {
        public int ProjectId { get; set; }
        public string DisplayName { get; set; } = "";
    }
}
