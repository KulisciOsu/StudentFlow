using System.Text.Json;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.IntegrationTests;

public class UserAccountStoreTests
{
    [Theory]
    [InlineData("Jan1", "Novák")]
    [InlineData("Jan", "Novák-Svoboda")]
    [InlineData("O'Neill", "Novák")]
    [InlineData("Jan", "Novák🦉")]
    public async Task Create_RejectsDigitsAndSymbolsEvenWithoutRegistrationController(string firstName, string lastName)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        await Assert.ThrowsAsync<ArgumentException>(() => store.CreateAsync(new(firstName, lastName, "invalid-name@example.test", "hash", AccountType.Member)));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }

    [Fact]
    public async Task PrivacyAcceptance_IsStoredAtomicallyAndDuplicateDoesNotReplaceIt()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var acceptance = new PrivacyAcceptance("2026-11-20", DateTimeOffset.Parse("2026-11-20T10:00:00Z"));
        Assert.Equal(CreateUserStatus.Created, (await store.CreateAsync(new("A", "Test", "privacy@example.test", "hash", AccountType.Member, acceptance))).Status);
        Assert.Equal(CreateUserStatus.EmailAlreadyRegistered, (await store.CreateAsync(new("B", "Test", "PRIVACY@example.test", "other-hash", AccountType.Member, new("other", acceptance.AcceptedAt.AddDays(1))))).Status);
        Assert.Equal(acceptance.Version, await db.ScalarAsync<string>("SELECT privacy_policy_version FROM app_user"));
        Assert.Equal(acceptance.AcceptedAt.UtcDateTime, await db.ScalarAsync<DateTime>("SELECT privacy_accepted_at FROM app_user"));
    }

    [Fact]
    public async Task PrivacyMigration_PreservesExistingAccountWithoutInventedAcceptance()
    {
        await using var db = await PostgresTestDatabase.CreateAsync(legacy: true);
        await db.ApplyMigrationAsync("V003__authentication_foundation.sql");
        await db.ExecuteAsync("""
            BEGIN;
            INSERT INTO app_user (email, display_name, password_hash) VALUES ('legacy@example.test', 'Starší účet', 'legacy-hash');
            INSERT INTO user_role (user_id, role_id) SELECT u.user_id, r.role_id FROM app_user u CROSS JOIN app_role r WHERE r.role_code = 'STUDENT';
            COMMIT;
            """);
        await db.ApplyMigrationAsync("V20261120_1200__registration_privacy_acceptance.sql");
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user WHERE privacy_policy_version IS NULL AND privacy_accepted_at IS NULL AND password_hash = 'legacy-hash'"));
    }

    [Fact]
    public async Task Create_ReturnsDatabaseCanonicalEmail_ForUnicodeCaseAndWhitespace()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var result = await store.CreateAsync(new("A", "Test", "\u00A0\u0130@EXAMPLE.TEST\u3000", "hash", AccountType.Member));
        var storedEmail = await db.ScalarAsync<string>("SELECT email FROM app_user");
        Assert.Equal(storedEmail, result.User!.Email);
        Assert.Equal(result.User, (await store.FindByEmailAsync("\u0130@example.test"))!.User);
    }

    [Theory]
    [InlineData(AccountType.Member, "STUDENT")]
    [InlineData(AccountType.Teacher, "TEACHER")]
    public async Task Create_NormalizesInputAndLoadsExactlyOneType(AccountType type, string code)
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var created = await store.CreateAsync(new("Jan", "Novák", " Jan.Novak@Example.test ", "opaque-hash", type));
        Assert.Equal(CreateUserStatus.Created, created.Status);
        Assert.NotNull(created.User);
        Assert.Equal("Jan Novák", created.User.DisplayName);
        Assert.Equal("jan.novak@example.test", created.User.Email);
        Assert.True(created.User.IsActive);
        Assert.Equal(type, created.User.AccountType);
        Assert.Equal(created.User, await store.FindByIdAsync(created.User.Id));
        var loaded = await store.FindByEmailAsync(" JAN.NOVAK@EXAMPLE.TEST ");
        Assert.Equal("opaque-hash", loaded!.PasswordHash);
        Assert.DoesNotContain("opaque-hash", JsonSerializer.Serialize(loaded));
        Assert.DoesNotContain("opaque-hash", loaded.ToString());
        Assert.Equal(new[] { code }, await store.GetRoleCodesAsync(created.User.Id));
        Assert.Null(await store.FindByEmailAsync("' OR 1=1 --"));
        Assert.Null(await store.FindByIdAsync(long.MaxValue));
    }

    [Fact]
    public async Task ConcurrentDuplicateRegistrations_CreateOneAccountAndOneRole()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var results = await Task.WhenAll(
            store.CreateAsync(new("A", "Test", "same@example.test", "hash-a", AccountType.Member)),
            store.CreateAsync(new("B", "Test", " SAME@EXAMPLE.TEST ", "hash-b", AccountType.Teacher)));
        Assert.Single(results, r => r.Status == CreateUserStatus.Created);
        Assert.Single(results, r => r.Status == CreateUserStatus.EmailAlreadyRegistered && r.User is null);
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(1L, await db.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    [Fact]
    public async Task MissingRole_RollsBackInsertedAccount()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        await db.ExecuteAsync("DELETE FROM app_role WHERE role_code = 'TEACHER'");
        var store = new PostgresUserAccountStore(db.DataSource);
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateAsync(new("A", "Test", "a@example.test", "hash", AccountType.Teacher)));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    [Fact]
    public async Task PasswordRehash_UsesExpectedHashAndPreservesAccountTypeAndActivity()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var created = await store.CreateAsync(new("A", "Test", "a@example.test", "old-hash", AccountType.Teacher));
        var id = created.User!.Id;
        await db.ExecuteAsync($"UPDATE app_user SET is_active = false WHERE user_id = {id}");
        Assert.True(await store.UpdatePasswordHashAsync(id, "old-hash", "new-hash"));
        Assert.False(await store.UpdatePasswordHashAsync(id, "old-hash", "stale-hash"));
        Assert.False(await store.UpdatePasswordHashAsync(long.MaxValue, "old-hash", "new-hash"));
        var loaded = await store.FindByEmailAsync("a@example.test");
        Assert.Equal("new-hash", loaded!.PasswordHash);
        Assert.False(loaded.User.IsActive);
        Assert.Equal(AccountType.Teacher, loaded.User.AccountType);
    }

    [Fact]
    public async Task InternalRole_DoesNotBecomeAnotherAccountType()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var created = await store.CreateAsync(new("A", "Test", "a@example.test", "hash", AccountType.Member));
        await db.ExecuteAsync($"INSERT INTO user_role(user_id,role_id) SELECT {created.User!.Id},role_id FROM app_role WHERE role_code='ADMIN'");
        Assert.Equal(new[] { "ADMIN", "STUDENT" }, await store.GetRoleCodesAsync(created.User.Id));
        Assert.Equal(AccountType.Member, (await store.FindByIdAsync(created.User.Id))!.AccountType);
    }

    [Fact]
    public async Task InvalidTypeAndCancelledWrite_DoNotCreateAccount()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.CreateAsync(new("A", "Test", "a@example.test", "hash", (AccountType)99)));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CreateAsync(new("A", "Test", "a@example.test", "hash", AccountType.Member), cancelled.Token));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }
}
