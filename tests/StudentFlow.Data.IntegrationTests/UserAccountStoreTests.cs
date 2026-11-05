using System.Text.Json;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.IntegrationTests;

public class UserAccountStoreTests
{
    [Fact]
    public async Task Create_ReturnsDatabaseCanonicalEmail_ForUnicodeCaseAndWhitespace()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var result = await store.CreateAsync(new("A", "\u00A0\u0130@EXAMPLE.TEST\u3000", "hash", AccountType.Member));
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
        var created = await store.CreateAsync(new("  Jan Novák  ", " Jan.Novak@Example.test ", "opaque-hash", type));
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
            store.CreateAsync(new("A", "same@example.test", "hash-a", AccountType.Member)),
            store.CreateAsync(new("B", " SAME@EXAMPLE.TEST ", "hash-b", AccountType.Teacher)));
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateAsync(new("A", "a@example.test", "hash", AccountType.Teacher)));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM user_role"));
    }

    [Fact]
    public async Task PasswordRehash_UsesExpectedHashAndPreservesAccountTypeAndActivity()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        var created = await store.CreateAsync(new("A", "a@example.test", "old-hash", AccountType.Teacher));
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
        var created = await store.CreateAsync(new("A", "a@example.test", "hash", AccountType.Member));
        await db.ExecuteAsync($"INSERT INTO user_role(user_id,role_id) SELECT {created.User!.Id},role_id FROM app_role WHERE role_code='ADMIN'");
        Assert.Equal(new[] { "ADMIN", "STUDENT" }, await store.GetRoleCodesAsync(created.User.Id));
        Assert.Equal(AccountType.Member, (await store.FindByIdAsync(created.User.Id))!.AccountType);
    }

    [Fact]
    public async Task InvalidTypeAndCancelledWrite_DoNotCreateAccount()
    {
        await using var db = await PostgresTestDatabase.CreateAsync();
        var store = new PostgresUserAccountStore(db.DataSource);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.CreateAsync(new("A", "a@example.test", "hash", (AccountType)99)));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CreateAsync(new("A", "a@example.test", "hash", AccountType.Member), cancelled.Token));
        Assert.Equal(0L, await db.ScalarAsync<long>("SELECT count(*) FROM app_user"));
    }
}
