using Npgsql;

namespace StudentFlow.Data.IntegrationTests;

/// <summary>
/// Testy omezení tabulky project_member po migraci V006 a migraci správy členů (KAN-93).
/// </summary>
public class ProjectMemberMigrationTests
{
    private const string Migration = "V20261114_2156__project_member_management.sql";
    private const int ProjectId = 1;

    /// <summary>
    /// Migrace přidá jen nepovinný sloupec added_by; existující členství zůstanou beze změny a bez autora.
    /// </summary>
    [Fact]
    public async Task Migration_ExistingMemberships_KeepDataAndGetEmptyAddedBy()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(dashboard: true);
        await SeedAsync(database);
        await AddMemberAsync(database, "petra@example.test", isLeader: true);

        await database.ApplyMigrationAsync(Migration);

        Assert.Equal("project_id, user_id, is_leader, joined_at, added_by", await database.ScalarAsync<string>("""
            SELECT string_agg(column_name, ', ' ORDER BY ordinal_position)
            FROM information_schema.columns WHERE table_name = 'project_member'
            """));
        Assert.Equal("YES", await database.ScalarAsync<string>("""
            SELECT is_nullable FROM information_schema.columns
            WHERE table_name = 'project_member' AND column_name = 'added_by'
            """));
        Assert.Equal(1L, await database.ScalarAsync<long>(
            "SELECT count(*) FROM project_member WHERE is_leader AND added_by IS NULL"));
    }

    /// <summary>
    /// Studentský účet může být vedoucím i běžným členem; u člena se uloží, kdo ho přidal.
    /// </summary>
    [Fact]
    public async Task Insert_StudentLeaderAndMember_StoresAddedBy()
    {
        await using var database = await CreateDatabaseAsync();

        await AddMemberAsync(database, "petra@example.test", isLeader: true);
        await AddMemberAsync(database, "jan@example.test", isLeader: false, addedBy: "petra@example.test");

        Assert.Equal("jan@example.test", await database.ScalarAsync<string>("""
            SELECT member.email
            FROM project_member pm
            JOIN app_user member ON member.user_id = pm.user_id
            JOIN app_user adder ON adder.user_id = pm.added_by
            WHERE adder.email = 'petra@example.test'
            """));
    }

    /// <summary>
    /// Účet vyučujícího databáze do týmu nepřijme ani mimo aplikaci.
    /// </summary>
    [Fact]
    public async Task Insert_TeacherAccount_IsRejected()
    {
        await using var database = await CreateDatabaseAsync();

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            AddMemberAsync(database, "eva@example.test", isLeader: false));

        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("project_member_student_only", error.ConstraintName);
        Assert.Equal(0L, await CountMembersAsync(database));
    }

    /// <summary>
    /// Ani přepsáním existujícího členství nelze do týmu dostat účet vyučujícího.
    /// </summary>
    [Fact]
    public async Task Update_MembershipToTeacherAccount_IsRejected()
    {
        await using var database = await CreateDatabaseAsync();
        await AddMemberAsync(database, "jan@example.test", isLeader: false);

        var error = await Assert.ThrowsAsync<PostgresException>(() => database.ExecuteAsync("""
            UPDATE project_member
            SET user_id = (SELECT user_id FROM app_user WHERE email = 'eva@example.test')
            WHERE user_id = (SELECT user_id FROM app_user WHERE email = 'jan@example.test')
            """));

        Assert.Equal("project_member_student_only", error.ConstraintName);
        Assert.Equal("jan@example.test", await database.ScalarAsync<string>(
            "SELECT email FROM project_member JOIN app_user USING (user_id)"));
    }

    /// <summary>
    /// Kontrola probíhá až při COMMIT: nový účet, jeho členství a typ lze vložit v jedné transakci v libovolném pořadí.
    /// </summary>
    [Fact]
    public async Task Insert_AccountMembershipAndTypeInOneTransaction_IsCheckedAtCommit()
    {
        await using var database = await CreateDatabaseAsync();

        await database.ExecuteAsync($"""
            BEGIN;
            INSERT INTO app_user (email, display_name, password_hash)
            VALUES ('lucie@example.test', 'Lucie Černá', 'not-set');
            INSERT INTO project_member (project_id, user_id)
            SELECT {ProjectId}, user_id FROM app_user WHERE email = 'lucie@example.test';
            INSERT INTO user_role (user_id, role_id)
            SELECT user_id, (SELECT role_id FROM app_role WHERE role_code = 'STUDENT')
            FROM app_user WHERE email = 'lucie@example.test';
            COMMIT;
            """);

        Assert.Equal(1L, await CountMembersAsync(database));
    }

    /// <summary>
    /// Stejný uživatel nemůže být v projektu dvakrát (pravidlo 4: duplicitní řádek nevznikne).
    /// </summary>
    [Fact]
    public async Task Insert_DuplicateMembership_IsRejected()
    {
        await using var database = await CreateDatabaseAsync();
        await AddMemberAsync(database, "jan@example.test", isLeader: false);

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            AddMemberAsync(database, "jan@example.test", isLeader: false));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal("pk_project_member", error.ConstraintName);
        Assert.Equal(1L, await CountMembersAsync(database));
    }

    /// <summary>
    /// Projekt nemůže mít druhého vedoucího (pravidlo 1: právě jeden vedoucí).
    /// </summary>
    [Fact]
    public async Task Insert_SecondLeader_IsRejected()
    {
        await using var database = await CreateDatabaseAsync();
        await AddMemberAsync(database, "petra@example.test", isLeader: true);

        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            AddMemberAsync(database, "jan@example.test", isLeader: true));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal("ux_project_member_leader", error.ConstraintName);
    }

    /// <summary>
    /// Smazání účtu, který člena přidal, členství nezruší; údaj o autorovi se jen vymaže.
    /// </summary>
    [Fact]
    public async Task Delete_AccountThatAddedMember_KeepsMembershipAndClearsAddedBy()
    {
        await using var database = await CreateDatabaseAsync();
        await AddMemberAsync(database, "jan@example.test", isLeader: false, addedBy: "tomas@example.test");

        await database.ExecuteAsync("DELETE FROM app_user WHERE email = 'tomas@example.test'");

        Assert.Equal(1L, await database.ScalarAsync<long>("""
            SELECT count(*) FROM project_member JOIN app_user USING (user_id)
            WHERE email = 'jan@example.test' AND added_by IS NULL
            """));
    }

    /// <summary>
    /// Připraví databázi s migracemi V001–V004, V006 a migrací správy členů, se studenty, vyučující a projektem.
    /// </summary>
    private static async Task<PostgresTestDatabase> CreateDatabaseAsync()
    {
        var database = await PostgresTestDatabase.CreateAsync(dashboard: true);

        try
        {
            await database.ApplyMigrationAsync(Migration);
            await SeedAsync(database);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Vloží tři studenty, jednu vyučující a projekt; typ účtu se vkládá ve stejné transakci jako účet (V003).
    /// </summary>
    private static Task SeedAsync(PostgresTestDatabase database) => database.ExecuteAsync("""
        BEGIN;
        INSERT INTO app_user (email, display_name, password_hash)
        VALUES
            ('petra@example.test', 'Petra Svobodová', 'not-set'),
            ('jan@example.test', 'Jan Novák', 'not-set'),
            ('tomas@example.test', 'Tomáš Dvořák', 'not-set'),
            ('eva@example.test', 'Eva Horáková', 'not-set');
        INSERT INTO user_role (user_id, role_id)
        SELECT app_user.user_id, app_role.role_id
        FROM app_user
        JOIN app_role ON app_role.role_code =
            CASE WHEN app_user.email = 'eva@example.test' THEN 'TEACHER' ELSE 'STUDENT' END;
        INSERT INTO project (name, team_name) VALUES ('Rezervační systém knihovny', 'Tým Sova');
        COMMIT;
        """);

    /// <summary>
    /// Vloží členství účtu podle e-mailu, volitelně s autorem přidání.
    /// </summary>
    private static Task AddMemberAsync(PostgresTestDatabase database, string email, bool isLeader, string? addedBy = null) =>
        database.ExecuteAsync(addedBy is null
            ? $"""
              INSERT INTO project_member (project_id, user_id, is_leader)
              SELECT {ProjectId}, user_id, {isLeader} FROM app_user WHERE email = '{email}'
              """
            : $"""
              INSERT INTO project_member (project_id, user_id, is_leader, added_by)
              SELECT {ProjectId}, member.user_id, {isLeader}, adder.user_id
              FROM app_user member, app_user adder
              WHERE member.email = '{email}' AND adder.email = '{addedBy}'
              """);

    /// <summary>
    /// Počet členství v testovacím projektu.
    /// </summary>
    private static Task<long> CountMembersAsync(PostgresTestDatabase database) =>
        database.ScalarAsync<long>($"SELECT count(*) FROM project_member WHERE project_id = {ProjectId}");
}
