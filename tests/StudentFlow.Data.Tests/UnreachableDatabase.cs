using Npgsql;

namespace StudentFlow.Data.Tests;

/// <summary>
/// Zdroj připojení na adresu, kde žádná databáze neběží; připojení selže okamžitě.
/// </summary>
/// <remarks>
/// Neplatný vstup musí datová vrstva odmítnout dřív, než se k databázi připojí. Platný vstup naopak
/// projde validací a skončí chybou připojení (<see cref="NpgsqlException"/>), čímž testy ověřují,
/// že kontrola vstupu proběhla opravdu před přístupem k databázi.
/// </remarks>
internal static class UnreachableDatabase
{
    /// <summary>Vytvoří zdroj připojení na nepoužívaný port lokálního počítače.</summary>
    public static NpgsqlDataSource Create() => NpgsqlDataSource.Create(
        "Host=127.0.0.1;Port=1;Database=studentflow_unit;Username=unit;Password=unit;Timeout=3;Pooling=false");
}
