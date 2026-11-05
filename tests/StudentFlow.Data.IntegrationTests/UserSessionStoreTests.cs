using System.Security.Cryptography;
using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.IntegrationTests;

public class UserSessionStoreTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private static async Task<long> UserAsync(PostgresTestDatabase database) =>
        (await new PostgresUserAccountStore(database.DataSource).CreateAsync(new("Jan Novák", "jan@example.test", "hash-v1", AccountType.Member))).User!.Id;

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 720)]
    public async Task CreateAndLoad_KeepAbsoluteLifetimeAndResolveCurrentOwner(bool persistent, int hours)
    {
        await using var database = await PostgresTestDatabase.CreateAsync(); var id = await UserAsync(database);
        var store = new PostgresUserSessionStore(database.DataSource); var hash = RandomNumberGenerator.GetBytes(32);
        var created = await store.CreateAsync(id, "hash-v1", null, hash, persistent, null, Now);
        Assert.NotNull(created); Assert.Equal(Now.AddHours(hours), created.Session.ExpiresAt);
        await database.ExecuteAsync("UPDATE app_user SET display_name = 'Aktuální jméno'");
        var loaded = await store.FindActiveAsync(hash, Now.AddHours(1));
        Assert.Equal("Aktuální jméno", loaded!.User.DisplayName);
        Assert.Equal(created.Session, loaded.Session);
        Assert.Null(await store.FindActiveAsync(hash, Now.AddHours(hours)));
        Assert.Null(await store.FindActiveAsync(RandomNumberGenerator.GetBytes(32), Now));
    }

    [Fact]
    public async Task ChangedPasswordOrDisabledOwner_CannotCreateSessionOrRevokePreviousOne()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(); var id = await UserAsync(database);
        var store = new PostgresUserSessionStore(database.DataSource); var first = RandomNumberGenerator.GetBytes(32);
        await store.CreateAsync(id, "hash-v1", null, first, false, null, Now);
        await database.ExecuteAsync("UPDATE app_user SET password_hash = 'hash-v2'");
        Assert.Null(await store.CreateAsync(id, "hash-v1", "replacement", RandomNumberGenerator.GetBytes(32), false, first, Now));
        await database.ExecuteAsync("UPDATE app_user SET is_active = false");
        Assert.Null(await store.CreateAsync(id, "hash-v2", null, RandomNumberGenerator.GetBytes(32), false, first, Now));
        Assert.Null(await store.FindActiveAsync(first, Now));
        Assert.Equal(1L, await database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
        Assert.Equal(0L, await database.ScalarAsync<long>("SELECT count(*) FROM user_session WHERE revoked_at IS NOT NULL"));
        Assert.Equal("hash-v2", await database.ScalarAsync<string>("SELECT password_hash FROM app_user"));
    }

    [Fact]
    public async Task FailedInsert_RollsBackRehashAndPreservesPreviousSession()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(); var id = await UserAsync(database);
        var store = new PostgresUserSessionStore(database.DataSource); var hash = RandomNumberGenerator.GetBytes(32);
        await store.CreateAsync(id, "hash-v1", null, hash, false, null, Now);
        var error = await Assert.ThrowsAsync<PostgresException>(() => store.CreateAsync(id, "hash-v1", "hash-v2", hash, false, hash, Now));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
        Assert.Equal("hash-v1", await database.ScalarAsync<string>("SELECT password_hash FROM app_user"));
        Assert.NotNull(await store.FindActiveAsync(hash, Now));
        Assert.Equal(1L, await database.ScalarAsync<long>("SELECT count(*) FROM user_session"));
    }

    [Fact]
    public async Task Revoke_IsIdempotentAndKeepsOtherSessionActive()
    {
        await using var database = await PostgresTestDatabase.CreateAsync(); var id = await UserAsync(database);
        var store = new PostgresUserSessionStore(database.DataSource);
        var first = RandomNumberGenerator.GetBytes(32); var other = RandomNumberGenerator.GetBytes(32);
        await store.CreateAsync(id, "hash-v1", null, first, false, null, Now);
        await store.CreateAsync(id, "hash-v1", null, other, false, null, Now);
        await store.RevokeAsync(first, Now.AddMinutes(1)); await store.RevokeAsync(first, Now.AddMinutes(2));
        await store.RevokeAsync(RandomNumberGenerator.GetBytes(32), Now);
        Assert.Null(await store.FindActiveAsync(first, Now.AddMinutes(3)));
        Assert.NotNull(await store.FindActiveAsync(other, Now.AddMinutes(3)));
        Assert.Equal(Now.AddMinutes(1).UtcDateTime, await database.ScalarAsync<DateTime>("SELECT revoked_at FROM user_session WHERE revoked_at IS NOT NULL"));
    }
}
