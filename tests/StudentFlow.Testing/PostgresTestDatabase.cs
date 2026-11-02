using Npgsql;

namespace StudentFlow.Testing;

public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly string adminConnectionString;
    private readonly string name;
    public NpgsqlDataSource DataSource { get; }
    public string ConnectionString { get; }

    private PostgresTestDatabase(string adminConnectionString, string name)
    {
        this.adminConnectionString = adminConnectionString;
        this.name = name;
        ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = name }.ConnectionString;
        DataSource = NpgsqlDataSource.Create(ConnectionString);
    }

    public static async Task<PostgresTestDatabase> CreateAsync(bool legacy = false)
    {
        var connectionString = Environment.GetEnvironmentVariable("STUDENTFLOW_TEST_DATABASE")
            ?? throw new InvalidOperationException("Set STUDENTFLOW_TEST_DATABASE to a dedicated PostgreSQL 17 server with CREATEDB permission. These tests never use the application's connection string.");
        var name = "studentflow_test_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(connectionString))
        {
            await admin.OpenAsync();
            if (admin.PostgreSqlVersion.Major != 17) throw new InvalidOperationException("Tests require PostgreSQL 17.");
            await using var create = new NpgsqlCommand($"CREATE DATABASE {name}", admin);
            await create.ExecuteNonQueryAsync();
        }
        var database = new PostgresTestDatabase(connectionString, name);
        try
        {
            await database.ApplyMigrationAsync("V001__initial_schema.sql");
            await database.ApplyMigrationAsync("V002__seed_roles.sql");
            if (!legacy) await database.ApplyMigrationAsync("V003__authentication_foundation.sql");
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public Task ApplyMigrationAsync(string file) => ExecuteAsync(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Migrations", file)));

    public async Task ExecuteAsync(string sql)
    {
        await using var command = DataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        await using var command = DataSource.CreateCommand(sql);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await using var admin = new NpgsqlConnection(adminConnectionString);
        await admin.OpenAsync();
        // Název je interně generované UUID; žádný název produkční DB se zde nepřijímá.
        await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
