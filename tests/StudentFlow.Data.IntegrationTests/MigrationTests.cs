using Npgsql;
using System.Data;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.IntegrationTests;

public class MigrationTests
{
    [Fact]
    public async Task FreshDatabase_CreatesNormalizedEmailAndSessionStorage()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM information_schema.tables WHERE table_name = 'user_session'"));
        Assert.Equal("ALWAYS", await db.ScalarAsync<string>("SELECT is_generated FROM information_schema.columns WHERE table_name = 'app_user' AND column_name = 'normalized_email'"));
    }

    [Fact]
    public async Task Upgrade_PreservesAccountIdHashAndType_AndNormalizesEmail()
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ExecuteAsync("""
            INSERT INTO app_user(email, display_name, password_hash) VALUES (' Jan.Novak@Example.test ', 'Jan Novák', 'existing-hash');
            INSERT INTO user_role(user_id, role_id) SELECT u.user_id, r.role_id FROM app_user u CROSS JOIN app_role r WHERE r.role_code = 'TEACHER';
            """);
        var id = await db.ScalarAsync<long>("SELECT user_id FROM app_user");
        await db.ApplyMigrationAsync("V003__authentication_foundation.sql");
        var loaded = await new PostgresUserAccountStore(db.DataSource).FindByEmailAsync("JAN.NOVAK@example.test");
        Assert.NotNull(loaded);
        Assert.Equal(id, loaded.User.Id);
        Assert.Equal("existing-hash", loaded.PasswordHash);
        Assert.Equal("jan.novak@example.test", loaded.User.Email);
        Assert.Equal(AccountType.Teacher, loaded.User.AccountType);
    }

    [Fact]
    public async Task Upgrade_EmailCollision_ReportsFailureAndLeavesOriginalSchemaAndAccounts()
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ExecuteAsync("""
            INSERT INTO app_user(email, display_name, password_hash) VALUES ('a@example.test', 'A', 'hash'), (' A@EXAMPLE.TEST ', 'B', 'hash');
            """);
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ApplyMigrationAsync("V003__authentication_foundation.sql"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal(2L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE table_name = 'app_user' AND column_name = 'normalized_email'"));
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE email = ' A@EXAMPLE.TEST '"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upgrade_MissingOrDoubleAccountType_FailsWithoutChoosingType(bool both)
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ExecuteAsync("INSERT INTO app_user(email, display_name, password_hash) VALUES ('a@example.test', 'A', 'hash')");
        if (both) await db.ExecuteAsync("INSERT INTO user_role SELECT 1, role_id, now(), NULL FROM app_role WHERE role_code IN ('STUDENT', 'TEACHER')");
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ApplyMigrationAsync("V003__authentication_foundation.sql"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, error.SqlState);
        Assert.Equal(both ? 2L : 0L, await db.ScalarAsync<long>("SELECT count(*) FROM user_role"));
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task AccountWithoutType_CannotCommit()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("INSERT INTO app_user(email,display_name,password_hash) VALUES ('a@example.test','A','hash')"));
        Assert.Equal("user_account_type_exactly_one", error.ConstraintName);
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Theory]
    [InlineData("\t", "hash", "app_user_display_name_not_blank")]
    [InlineData("\u00A0", "hash", "app_user_display_name_not_blank")]
    [InlineData("A", "\t", "app_user_password_hash_not_blank")]
    public async Task WhitespaceOnlyNameOrHash_IsRejectedByDatabase(string name, string hash, string constraint)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        await using var command = db.DataSource.CreateCommand("INSERT INTO app_user(email,display_name,password_hash) VALUES ('a@example.test', @name, @hash)");
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("hash", hash);
        var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(constraint, error.ConstraintName);
    }

    [Fact]
    public async Task LastTypeCannotBeDeleted_SecondTypeCannotBeAdded_AndCodesCannotBeRenamed()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var created = await new PostgresUserAccountStore(db.DataSource).CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        var id = created.User!.Id;
        var missing = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync($"DELETE FROM user_role WHERE user_id = {id}"));
        Assert.Equal("user_account_type_exactly_one", missing.ConstraintName);
        var both = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync($"INSERT INTO user_role(user_id, role_id) SELECT {id}, role_id FROM app_role WHERE role_code = 'TEACHER'"));
        Assert.Equal("user_account_type_exactly_one", both.ConstraintName);
        var renamed = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("UPDATE app_role SET role_code = 'OTHER' WHERE role_code = 'STUDENT'"));
        Assert.Equal("account_type_codes_immutable", renamed.ConstraintName);
        Assert.Equal(new[] { "STUDENT" }, await new PostgresUserAccountStore(db.DataSource).GetRoleCodesAsync(id));
    }

    [Fact]
    public async Task NormalizedEmailUniqueness_IsEnforcedForDirectSqlWrites()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        await new PostgresUserAccountStore(db.DataSource).CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("INSERT INTO app_user(email,display_name,password_hash) VALUES (' A@EXAMPLE.TEST ','B','hash')"));
        Assert.Equal("app_user_normalized_email_key", error.ConstraintName);
    }

    [Fact]
    public async Task NormalizedEmailUniqueness_AlsoTrimsUnicodeWhitespaceInDirectSql()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        await new PostgresUserAccountStore(db.DataSource).CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("INSERT INTO app_user(email,display_name,password_hash) VALUES (U&'\\00A0A@EXAMPLE.TEST\\3000','B','hash')"));
        Assert.Equal("app_user_normalized_email_key", error.ConstraintName);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    public async Task ConcurrentTypeChanges_CannotCommitBothTypes(IsolationLevel isolationLevel)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var created = await store.CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        var id = created.User!.Id;
        await using (var first = await db.DataSource.OpenConnectionAsync())
        await using (var second = await db.DataSource.OpenConnectionAsync())
        await using (var firstTransaction = await first.BeginTransactionAsync())
        await using (var secondTransaction = await second.BeginTransactionAsync(isolationLevel))
        {
            await using (var snapshot = new NpgsqlCommand("SELECT count(*) FROM user_role", second, secondTransaction))
                await snapshot.ExecuteScalarAsync();
            await using (var replace = new NpgsqlCommand($"DELETE FROM user_role WHERE user_id={id}; INSERT INTO user_role(user_id,role_id) SELECT {id},role_id FROM app_role WHERE role_code='TEACHER'", first, firstTransaction))
                await replace.ExecuteNonQueryAsync();
            await using var conflicting = new NpgsqlCommand($"INSERT INTO user_role(user_id,role_id) SELECT {id},role_id FROM app_role WHERE role_code='STUDENT'", second, secondTransaction);
            var insert = conflicting.ExecuteNonQueryAsync();
            await firstTransaction.CommitAsync();
            var error = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await insert;
                await secondTransaction.CommitAsync();
            });
            Assert.Contains(error.SqlState, new[] { PostgresErrorCodes.CheckViolation, PostgresErrorCodes.SerializationFailure });
        }
        Assert.Equal(new[] { "TEACHER" }, await store.GetRoleCodesAsync(id));
        Assert.Equal(AccountType.Teacher, (await store.FindByIdAsync(id))!.AccountType);
    }

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 720)]
    public async Task Sessions_StoreOnlyHash_AndEnforceAbsoluteLifetime(bool persistent, int hours)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var account = await new PostgresUserAccountStore(db.DataSource).CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        var sql = $"""
            INSERT INTO user_session(user_id, token_hash, created_at, expires_at, persistent)
            VALUES ({account.User!.Id}, decode(repeat('ab',32),'hex'), '2026-11-02T12:00:00Z',
                    '2026-11-02T12:00:00Z'::timestamptz + interval '{hours} hours', {persistent.ToString().ToLowerInvariant()})
            """;
        await db.ExecuteAsync(sql);
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM user_session"));
        var lifetime = await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("UPDATE user_session SET expires_at = expires_at + interval '1 second'"));
        Assert.Equal("user_session_absolute_lifetime", lifetime.ConstraintName);
        await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("UPDATE user_session SET token_hash = decode('ab','hex')"));
        await Assert.ThrowsAsync<PostgresException>(() => db.ExecuteAsync("UPDATE user_session SET revoked_at = created_at - interval '1 second'"));
        await db.ExecuteAsync($"DELETE FROM app_user WHERE user_id = {account.User.Id}");
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM user_session"));
    }
}
