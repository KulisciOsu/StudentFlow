using StudentFlow.Core.Authentication;
using StudentFlow.Core.ProjectMembers;
using StudentFlow.Data.ProjectMembers;

namespace StudentFlow.Data.IntegrationTests;

/// <summary>
/// Testy úložiště členů projektu proti PostgreSQL s migracemi V001–V004, V006 a migrací správy členů.
/// </summary>
public class PostgresProjectMemberStoreTests
{
    private const string Migration = "V20261114_2156__project_member_management.sql";
    private const int ProjectId = 1;
    private const int MissingProjectId = 999;

    /// <summary>
    /// Členství se načte i s údaji účtu; nečlen ani neexistující projekt členství nemají.
    /// </summary>
    [Fact]
    public async Task FindMemberAsync_ReturnsOnlyExistingMembership()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var jan = await IdAsync(database, "jan@example.test");

        var member = await store.FindMemberAsync(ProjectId, jan);

        Assert.NotNull(member);
        Assert.Equal(new { UserId = jan, DisplayName = "Jan Novák", Email = "jan@example.test", IsLeader = false },
            new { member.UserId, member.DisplayName, member.Email, member.IsLeader });
        Assert.Equal(TimeSpan.Zero, member.JoinedAt.Offset);
        Assert.InRange(member.JoinedAt, DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.Null(await store.FindMemberAsync(ProjectId, await IdAsync(database, "lucie@example.test")));
        Assert.Null(await store.FindMemberAsync(MissingProjectId, jan));
    }

    /// <summary>
    /// Název týmu se vrátí jen pro existující projekt.
    /// </summary>
    [Fact]
    public async Task FindTeamNameAsync_ReturnsTeamNameOrNull()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);

        Assert.Equal("Tým Sova", await store.FindTeamNameAsync(ProjectId));
        Assert.Null(await store.FindTeamNameAsync(MissingProjectId));
    }

    /// <summary>
    /// Vedoucí je první, ostatní podle české abecedy: C před Č a H před Ch.
    /// </summary>
    [Fact]
    public async Task ListAsync_ReturnsLeaderFirstThenCzechAlphabeticalOrder()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);

        var members = await store.ListAsync(ProjectId);

        Assert.Equal(
            new[] { "Petra Svobodová", "Cyril Adámek", "Čeněk Dvořák", "Hynek Beneš", "Chrudoš Kratochvíl", "Jan Novák" },
            members.Select(member => member.DisplayName));
        Assert.Equal(new[] { true, false, false, false, false, false }, members.Select(member => member.IsLeader));
        Assert.Empty(await store.ListAsync(MissingProjectId));
    }

    /// <summary>
    /// Účet se najde podle normalizovaného e-mailu včetně typu a aktivity; neznámý e-mail nic nevrátí.
    /// </summary>
    [Fact]
    public async Task FindAccountByEmailAsync_UsesNormalizedEmailAndReturnsTypeAndActivity()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);

        var lucie = await store.FindAccountByEmailAsync("  LUCIE@Example.TEST ");
        var eva = await store.FindAccountByEmailAsync("eva@example.test");
        var martin = await store.FindAccountByEmailAsync("martin@example.test");

        Assert.Equal(new UserIdentity(await IdAsync(database, "lucie@example.test"), "Lucie Černá", "lucie@example.test",
            AccountType.Member, true), lucie);
        Assert.Equal(AccountType.Teacher, eva?.AccountType);
        Assert.False(martin?.IsActive);
        Assert.Null(await store.FindAccountByEmailAsync("nikdo@example.test"));
    }

    /// <summary>
    /// Vedoucí přidá studenta jako běžného člena a uloží se, kdo ho přidal.
    /// </summary>
    [Fact]
    public async Task AddAsync_LeaderAddsStudent_AddsRegularMemberWithAddedBy()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var petra = await IdAsync(database, "petra@example.test");
        var lucie = await IdAsync(database, "lucie@example.test");

        var result = await store.AddAsync(ProjectId, lucie, petra);

        Assert.Equal(AddMemberStatus.Added, result.Status);
        Assert.NotNull(result.Member);
        Assert.Equal(new { UserId = lucie, DisplayName = "Lucie Černá", IsLeader = false },
            new { result.Member.UserId, result.Member.DisplayName, result.Member.IsLeader });
        Assert.Equal(petra, await database.ScalarAsync<long>(
            $"SELECT added_by FROM project_member WHERE project_id = {ProjectId} AND user_id = {lucie}"));
    }

    /// <summary>
    /// Existujícího člena ani samotnou vedoucí nelze přidat znovu; jejich členství se nezmění.
    /// </summary>
    [Theory]
    [InlineData("jan@example.test")]
    [InlineData("petra@example.test")]
    public async Task AddAsync_ExistingMember_ReturnsAlreadyMemberWithoutChanges(string email)
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var before = await MembershipSnapshotAsync(database);

        var result = await store.AddAsync(ProjectId, await IdAsync(database, email), await IdAsync(database, "petra@example.test"));

        Assert.Equal(AddMemberStatus.AlreadyMember, result.Status);
        Assert.Null(result.Member);
        Assert.Equal(before, await MembershipSnapshotAsync(database));
    }

    /// <summary>
    /// Běžný člen ani nečlen nikoho nepřidá, i když úložiště zavolá přímo.
    /// </summary>
    [Theory]
    [InlineData("jan@example.test")]
    [InlineData("lucie@example.test")]
    public async Task AddAsync_CallerNotLeader_ReturnsNotLeaderAndAddsNobody(string callerEmail)
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var before = await MembershipSnapshotAsync(database);

        var result = await store.AddAsync(ProjectId, await IdAsync(database, "martin@example.test"),
            await IdAsync(database, callerEmail));

        Assert.Equal(AddMemberStatus.NotLeader, result.Status);
        Assert.Equal(before, await MembershipSnapshotAsync(database));
    }

    /// <summary>
    /// Účet vyučující odmítne databázová kontrola a úložiště to vrátí jako AccountNotStudent, ne jako chybu serveru.
    /// </summary>
    [Fact]
    public async Task AddAsync_TeacherAccount_ReturnsAccountNotStudent()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var before = await MembershipSnapshotAsync(database);

        var result = await store.AddAsync(ProjectId, await IdAsync(database, "eva@example.test"),
            await IdAsync(database, "petra@example.test"));

        Assert.Equal(AddMemberStatus.AccountNotStudent, result.Status);
        Assert.Equal(before, await MembershipSnapshotAsync(database));
    }

    /// <summary>
    /// Vedoucí odebere člena; smaže se jen členství a jeho úkol zůstane beze změny (pravidlo 7).
    /// </summary>
    [Fact]
    public async Task RemoveAsync_LeaderRemovesMember_RemovesOnlyMembershipAndKeepsTasks()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var jan = await IdAsync(database, "jan@example.test");

        var removed = await store.RemoveAsync(ProjectId, jan, await IdAsync(database, "petra@example.test"));

        Assert.True(removed);
        Assert.Null(await store.FindMemberAsync(ProjectId, jan));
        Assert.Equal(5, (await store.ListAsync(ProjectId)).Count);
        Assert.Equal(jan, await database.ScalarAsync<long>("SELECT assignee_id FROM task WHERE title = 'Návrh databázového modelu'"));
    }

    /// <summary>
    /// Vedoucího úložiště nikdy neodebere, ani když o to požádá on sám.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_Leader_IsNeverRemoved()
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var petra = await IdAsync(database, "petra@example.test");

        Assert.False(await store.RemoveAsync(ProjectId, petra, petra));
        Assert.True((await store.FindMemberAsync(ProjectId, petra))?.IsLeader);
    }

    /// <summary>
    /// Běžný člen nikoho neodebere a odebrání nečlena nic nezmění.
    /// </summary>
    [Theory]
    [InlineData("jan@example.test", "cyril@example.test")]
    [InlineData("petra@example.test", "lucie@example.test")]
    public async Task RemoveAsync_NotLeaderOrNotMember_RemovesNobody(string callerEmail, string memberEmail)
    {
        await using var database = await CreateDatabaseAsync();
        var store = new PostgresProjectMemberStore(database.DataSource);
        var before = await MembershipSnapshotAsync(database);

        var removed = await store.RemoveAsync(ProjectId, await IdAsync(database, memberEmail), await IdAsync(database, callerEmail));

        Assert.False(removed);
        Assert.Equal(before, await MembershipSnapshotAsync(database));
    }

    /// <summary>
    /// S pravidly z Core a skutečnou databází ztratí odebraný člen přístup hned při dalším požadavku (pravidlo 6).
    /// </summary>
    [Fact]
    public async Task Service_RemovedMember_LosesAccessImmediately()
    {
        await using var database = await CreateDatabaseAsync();
        var service = new ProjectMemberService(new PostgresProjectMemberStore(database.DataSource));
        var jan = await IdAsync(database, "jan@example.test");
        Assert.Equal(ListMembersStatus.Listed, (await service.ListAsync(ProjectId, jan)).Status);

        Assert.Equal(RemoveMemberStatus.Removed, await service.RemoveAsync(ProjectId, await IdAsync(database, "petra@example.test"), jan));

        var afterRemoval = await service.ListAsync(ProjectId, jan);
        Assert.Equal(ListMembersStatus.ProjectNotFound, afterRemoval.Status);
        Assert.Null(afterRemoval.TeamName);
    }

    /// <summary>
    /// Připraví databázi s migracemi, týmem „Tým Sova“, účty mimo tým a úkolem přiřazeným Janovi.
    /// </summary>
    private static async Task<PostgresTestDatabase> CreateDatabaseAsync()
    {
        var database = await PostgresTestDatabase.CreateAsync(dashboard: true);

        try
        {
            await database.ApplyMigrationAsync(Migration);
            await database.ExecuteAsync("""
                BEGIN;
                INSERT INTO app_user (email, display_name, password_hash, is_active)
                VALUES
                    ('petra@example.test', 'Petra Svobodová', 'not-set', TRUE),
                    ('jan@example.test', 'Jan Novák', 'not-set', TRUE),
                    ('cyril@example.test', 'Cyril Adámek', 'not-set', TRUE),
                    ('cenek@example.test', 'Čeněk Dvořák', 'not-set', TRUE),
                    ('hynek@example.test', 'Hynek Beneš', 'not-set', TRUE),
                    ('chrudos@example.test', 'Chrudoš Kratochvíl', 'not-set', TRUE),
                    ('lucie@example.test', 'Lucie Černá', 'not-set', TRUE),
                    ('martin@example.test', 'Martin Král', 'not-set', FALSE),
                    ('eva@example.test', 'Eva Horáková', 'not-set', TRUE);
                INSERT INTO user_role (user_id, role_id)
                SELECT app_user.user_id, app_role.role_id
                FROM app_user
                JOIN app_role ON app_role.role_code =
                    CASE WHEN app_user.email = 'eva@example.test' THEN 'TEACHER' ELSE 'STUDENT' END;
                INSERT INTO project (name, team_name) VALUES ('Rezervační systém knihovny', 'Tým Sova');
                INSERT INTO project_member (project_id, user_id, is_leader)
                SELECT 1, user_id, email = 'petra@example.test'
                FROM app_user
                WHERE email IN ('petra@example.test', 'jan@example.test', 'cyril@example.test',
                                'cenek@example.test', 'hynek@example.test', 'chrudos@example.test');
                INSERT INTO board_column (project_id, title, position) VALUES (1, 'K řešení', 0);
                INSERT INTO task (project_id, column_id, position, title, assignee_id)
                SELECT 1, board_column.column_id, 0, 'Návrh databázového modelu', app_user.user_id
                FROM board_column, app_user
                WHERE app_user.email = 'jan@example.test';
                COMMIT;
                """);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// ID účtu podle e-mailu.
    /// </summary>
    private static Task<long> IdAsync(PostgresTestDatabase database, string email) =>
        database.ScalarAsync<long>($"SELECT user_id FROM app_user WHERE email = '{email}'");

    /// <summary>
    /// Textový otisk všech členství včetně vedení a autora přidání; slouží k ověření, že se nic nezměnilo.
    /// </summary>
    private static Task<string> MembershipSnapshotAsync(PostgresTestDatabase database) => database.ScalarAsync<string>("""
        SELECT string_agg(project_id || ':' || user_id || ':' || is_leader || ':' || coalesce(added_by::text, '-'),
                          ',' ORDER BY project_id, user_id)
        FROM project_member
        """);
}
