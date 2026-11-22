using Npgsql;
using NpgsqlTypes;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.IntegrationTests;

public class RegistrationNameMigrationTests
{
    [Fact]
    public async Task Migration_PreservesLegacyNamesAndSessionsAndStoresNewPartsWithoutDuplicatingFullName()
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ApplyMigrationAsync("V003__authentication_foundation.sql");
        await db.ExecuteAsync("""
            BEGIN;
            INSERT INTO app_user(email, display_name, password_hash)
            VALUES ('legacy@example.test', 'Jan Pavel Novák', 'original-hash');
            INSERT INTO user_role(user_id, role_id)
            SELECT u.user_id, r.role_id FROM app_user u CROSS JOIN app_role r WHERE role_code = 'STUDENT';
            INSERT INTO user_session(user_id, token_hash, created_at, expires_at, persistent)
            SELECT user_id, decode(repeat('ab', 32), 'hex'), '2026-11-20T10:00:00Z', '2026-11-20T18:00:00Z', false
            FROM app_user;
            COMMIT;
            """);
        await db.ApplyMigrationAsync("V20261120_1200__registration_privacy_acceptance.sql");
        await db.ApplyMigrationAsync("V20261120_1300__separate_registration_names.sql");
        await db.ApplyMigrationAsync("V20261120_1400__registration_name_length_50.sql");
        var store = new PostgresUserAccountStore(db.DataSource);
        var legacy = await store.FindByEmailAsync("legacy@example.test");
        Assert.Equal("Jan Pavel Novák", legacy!.User.DisplayName);
        Assert.Equal("original-hash", legacy.PasswordHash);
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE first_name IS NULL AND last_name IS NULL"));
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE privacy_policy_version IS NULL AND privacy_accepted_at IS NULL"));
        var sessions = new PostgresUserSessionStore(db.DataSource);
        var tokenHash = Enumerable.Repeat((byte)0xab, 32).ToArray();
        var now = DateTimeOffset.Parse("2026-11-20T12:00:00Z");
        var session = await sessions.FindActiveAsync(tokenHash, now);
        Assert.NotNull(session);
        Assert.Equal(legacy.User, session.User);
        Assert.Equal(DateTimeOffset.Parse("2026-11-20T10:00:00Z"), session.Session.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2026-11-20T18:00:00Z"), session.Session.ExpiresAt);
        Assert.False(session.Session.Persistent);
        Assert.Equal(new[] { "STUDENT" }, await store.GetRoleCodesAsync(legacy.User.Id));
        await sessions.RevokeAsync(tokenHash, now);
        Assert.Null(await sessions.FindActiveAsync(tokenHash, now));

        var created = await store.CreateAsync(new("Žofie", "NovákováSvobodová", "new@example.test", "new-hash", AccountType.Member));
        Assert.Equal("Žofie NovákováSvobodová", created.User!.DisplayName);
        Assert.Equal(created.User, await store.FindByIdAsync(created.User.Id));
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE first_name = 'Žofie' AND last_name = 'NovákováSvobodová' AND display_name IS NULL"));
    }

    [Theory]
    [InlineData(null, "Novák")]
    [InlineData("Jan", null)]
    [InlineData("", "Novák")]
    [InlineData("Jan", "")]
    [InlineData(" Jan", "Novák")]
    [InlineData("Jan", "Novák ")]
    [InlineData("Jan Pavel", "Novák")]
    [InlineData("Jan", "Novák\tSvoboda")]
    [InlineData("Jan\nPavel", "Novák")]
    [InlineData("Jan", "Novák\u00A0Svoboda")]
    [InlineData("Jan\u0085Pavel", "Novák")]
    [InlineData("Jan", "Novák\u2003Svoboda")]
    [InlineData("Jan\u3000Pavel", "Novák")]
    [InlineData("Jan", "Novák\uFEFFSvoboda")]
    public async Task Database_RejectsIncompleteOrWhitespaceNameParts(string? firstName, string? lastName)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        await using var command = db.DataSource.CreateCommand("""
            INSERT INTO app_user(first_name, last_name, email, password_hash)
            VALUES (@first, @last, 'invalid@example.test', 'hash')
            """);
        command.Parameters.AddWithValue("first", NpgsqlDbType.Text, (object?)firstName ?? DBNull.Value);
        command.Parameters.AddWithValue("last", NpgsqlDbType.Text, (object?)lastName ?? DBNull.Value);
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal("app_user_name_parts_complete", error.ConstraintName);
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task Database_AcceptsFiftyUnicodeCharactersAndRejectsFiftyOne()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var name = string.Concat(Enumerable.Repeat("𐐀", 50));
        var created = await new PostgresUserAccountStore(db.DataSource).CreateAsync(new(name, name, "boundary@example.test", "hash", AccountType.Member));
        Assert.Equal($"{name} {name}", created.User!.DisplayName);
        Assert.Equal(50, await db.ScalarAsync<int>("SELECT length(first_name) FROM app_user"));
        Assert.Equal(50, await db.ScalarAsync<int>("SELECT length(last_name) FROM app_user"));
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("UPDATE app_user SET first_name = repeat('A', 51)"));
        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, error.SqlState);
        Assert.Equal(name, await db.ScalarAsync<string>("SELECT first_name FROM app_user"));
    }

    [Fact]
    public async Task LengthMigration_StopsWithoutTruncatingExistingLongNameParts()
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ApplyMigrationAsync("V003__authentication_foundation.sql");
        await db.ApplyMigrationAsync("V20261120_1300__separate_registration_names.sql");
        await db.ExecuteAsync("""
            BEGIN;
            INSERT INTO app_user(first_name, last_name, email, password_hash)
            VALUES (repeat('A', 51), 'Novák', 'long@example.test', 'hash');
            INSERT INTO user_role(user_id, role_id)
            SELECT u.user_id, r.role_id FROM app_user u CROSS JOIN app_role r WHERE role_code = 'STUDENT';
            COMMIT;
            """);
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ApplyMigrationAsync("V20261120_1400__registration_name_length_50.sql"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal(new string('A', 51), await db.ScalarAsync<string>("SELECT first_name FROM app_user"));
        Assert.Equal(160, await db.ScalarAsync<int>("SELECT character_maximum_length FROM information_schema.columns WHERE table_name = 'app_user' AND column_name = 'first_name'"));
    }
}
