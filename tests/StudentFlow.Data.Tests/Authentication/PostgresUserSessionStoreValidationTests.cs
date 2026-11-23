using Npgsql;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Data.Tests.Authentication;

/// <summary>
/// Jednotkové testy kontrol vstupu <see cref="PostgresUserSessionStore"/>, které proběhnou bez databáze.
/// </summary>
public class PostgresUserSessionStoreValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgresUserSessionStore store = new(UnreachableDatabase.Create());

    /// <summary>Hash tokenu session má vždy 32 bajtů (SHA-256).</summary>
    private static byte[] TokenHash(int length = 32) => new byte[length];

    /// <summary>Hash tokenu jiné délky se při založení session odmítne.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public async Task CreateAsync_InvalidTokenHash_Throws(int length)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            store.CreateAsync(1, "hash", null, TokenHash(length), false, null, Now));
    }

    /// <summary>Chybějící hash tokenu se odmítne.</summary>
    [Fact]
    public async Task CreateAsync_NullTokenHash_Throws()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            store.CreateAsync(1, "hash", null, null!, false, null, Now));
    }

    /// <summary>Hash předchozího tokenu musí mít také 32 bajtů.</summary>
    [Fact]
    public async Task CreateAsync_InvalidPreviousTokenHash_Throws()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            store.CreateAsync(1, "hash", null, TokenHash(), false, TokenHash(16), Now));
    }

    /// <summary>Očekávaný hash hesla a případný nový hash nesmí být prázdné.</summary>
    [Theory]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("hash", "")]
    [InlineData("hash", " ")]
    public async Task CreateAsync_BlankPasswordHash_Throws(string expectedHash, string? replacementHash)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            store.CreateAsync(1, expectedHash, replacementHash, TokenHash(), false, null, Now));
    }

    /// <summary>Platný vstup projde kontrolou a teprve potom se pokusí připojit k databázi.</summary>
    [Fact]
    public async Task CreateAsync_ValidInput_ReachesDatabase()
    {
        await Assert.ThrowsAnyAsync<NpgsqlException>(() =>
            store.CreateAsync(1, "hash", null, TokenHash(), true, TokenHash(), Now));
    }

    /// <summary>Hledání session podle hashe nesprávné délky se odmítne.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task FindActiveAsync_InvalidTokenHash_Throws(int length)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.FindActiveAsync(TokenHash(length), Now));
    }

    /// <summary>Zneplatnění session podle hashe nesprávné délky se odmítne.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    public async Task RevokeAsync_InvalidTokenHash_Throws(int length)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => store.RevokeAsync(TokenHash(length), Now));
    }
}
