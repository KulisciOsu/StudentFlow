using System.Text.RegularExpressions;

namespace StudentFlow.Data.IntegrationTests;

/// <summary>
/// Kontroluje názvy migračních souborů, aby dvě větve nevytvořily migraci se stejnou verzí.
/// Nepotřebuje databázi.
/// </summary>
public class MigrationFileTests
{
    /// <summary>
    /// Původní pořadová čísla V001 až V007, nové migrace s časovým razítkem VRRRRMMDD_HHMM.
    /// </summary>
    private static readonly Regex NamePattern = new(@"^V(?<version>\d{3}|\d{8}_\d{4})__[a-z0-9_]+\.sql$");

    /// <summary>
    /// Ověří, že všechny migrace mají platný název a žádné dvě nemají stejnou verzi.
    /// </summary>
    [Fact]
    public void Migrations_HaveValidAndUniqueVersions()
    {
        var files = Directory
            .GetFiles(FindMigrationsDirectory(), "*.sql")
            .Select(path => Path.GetFileName(path))
            .ToList();

        var invalid = files
            .Where(file => !NamePattern.IsMatch(file))
            .ToList();

        var duplicates = files
            .Where(file => NamePattern.IsMatch(file))
            .GroupBy(file => NamePattern.Match(file).Groups["version"].Value)
            .Where(group => group.Count() > 1)
            .Select(group => string.Join(", ", group))
            .ToList();

        Assert.True(invalid.Count == 0, $"Neplatný název migrace: {string.Join(", ", invalid)}. Nové migrace pojmenujte VRRRRMMDD_HHMM__popis.sql.");
        Assert.True(duplicates.Count == 0, $"Migrace se stejnou verzí: {string.Join("; ", duplicates)}. Přejmenujte migraci z větve, která se merguje později.");
    }

    /// <summary>
    /// Najde složku database/migrations ve zdrojovém repozitáři, ne kopii ve výstupu buildu.
    /// </summary>
    private static string FindMigrationsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var migrations = Path.Combine(directory.FullName, "database", "migrations");

            if (Directory.Exists(migrations))
            {
                return migrations;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Složka database/migrations nebyla nalezena.");
    }
}
