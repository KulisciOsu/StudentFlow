using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Testing;
using StudentFlow.Data.Authentication;

namespace StudentFlow.Api.IntegrationTests;

/// <summary>Spouští skutečnou HTTP pipeline nad izolovanou databází s řízeným časem a volitelnými výpadky.</summary>
internal sealed class RegistrationApp : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> factory;
    public PostgresTestDatabase Database { get; }
    public HttpClient Client { get; }
    public TestClock Clock { get; } = new();
    public ConcurrentQueue<string> Logs { get; } = new();
    public bool FailSessionReads { get; set; }
    public bool FailSessionCreates { get; set; }
    public bool FailSessionRevocations { get; set; }

    private RegistrationApp(PostgresTestDatabase database, int permitLimit, bool failWrites, int loginPermitLimit, bool includeHandoffExample)
    {
        Database = database;
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", database.ConnectionString);
            builder.UseSetting("Auth:RegistrationRateLimit:PermitLimit", permitLimit.ToString());
            builder.UseSetting("Auth:LoginRateLimit:PermitLimit", loginPermitLimit.ToString());
            builder.ConfigureLogging(logging => logging.AddProvider(new CapturedLogProvider(Logs)));
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                if (includeHandoffExample)
                    services.AddControllers().AddApplicationPart(typeof(StudentFlow.Api.Controllers.AccountSummaryController).Assembly);
                services.AddSingleton<TimeProvider>(Clock);
                services.RemoveAll<IUserSessionStore>();
                services.AddScoped<IUserSessionStore>(_ => new SessionStoreWithFailures(new PostgresUserSessionStore(Database.DataSource), this));
                if (failWrites)
                {
                    services.RemoveAll<IUserAccountStore>();
                    services.AddScoped<IUserAccountStore, UnavailableAccountStore>();
                }
            });
        });
        Client = CreateClient();
    }
    /// <summary>Vytvoří HTTPS klienta bez automatických přesměrování; vlastní cookie jar představuje další prohlížeč.</summary>
    public HttpClient CreateClient(bool cookies = true) => factory.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = cookies });

    /// <summary>Připraví testovací API s vlastní databází, nastavitelnými limity a volitelným integračním vzorem.</summary>
    public static async Task<RegistrationApp> CreateAsync(int permitLimit = 100, bool failWrites = false, int loginPermitLimit = 100, bool includeHandoffExample = false)
    {
        var database = await PostgresTestDatabase.CreateAsync();
        try { return new(database, permitLimit, failWrites, loginPermitLimit, includeHandoffExample); }
        catch { await database.DisposeAsync(); throw; }
    }

    public async Task<HttpResponseMessage> SetCsrfAsync()
        => await SetCsrfAsync(Client);

    /// <summary>Obnoví CSRF hlavičku podle aktuálních cookies klienta, například po změně přihlášení.</summary>
    public static async Task<HttpResponseMessage> SetCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/csrf");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json.GetProperty("token").GetString());
        return response;
    }

    public static Dictionary<string, object?> Valid(string email = "jan.novak@example.test", string type = "STUDENT", string password = "moje-dlouhe-heslo-2026") => new()
    {
        ["displayName"] = " Jan Novák ", ["email"] = email, ["password"] = password, ["accountType"] = type
    };

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await factory.DisposeAsync();
        await Database.DisposeAsync();
    }

    private sealed class UnavailableAccountStore : IUserAccountStore
    {
        public Task<CreateUserResult> CreateAsync(NewUserAccount account, CancellationToken cancellationToken = default)
            => throw new NpgsqlException("sensitive-database-detail", new IOException("sensitive-input"));
        public Task<UserCredentials?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserIdentity?> FindByIdAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetRoleCodesAsync(long id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdatePasswordHashAsync(long id, string expectedHash, string newHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SessionStoreWithFailures(IUserSessionStore inner, RegistrationApp app) : IUserSessionStore
    {
        private static void Check(bool fail) { if (fail) throw new NpgsqlException("sensitive-session-detail", new IOException("sensitive-input")); }
        public Task<SessionIdentity?> CreateAsync(long userId, string expectedPasswordHash, string? replacementPasswordHash,
            byte[] tokenHash, bool persistent, byte[]? previousTokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        { Check(app.FailSessionCreates); return inner.CreateAsync(userId, expectedPasswordHash, replacementPasswordHash, tokenHash, persistent, previousTokenHash, now, cancellationToken); }
        public Task<SessionIdentity?> FindActiveAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        { Check(app.FailSessionReads); return inner.FindActiveAsync(tokenHash, now, cancellationToken); }
        public Task RevokeAsync(byte[] tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
        { Check(app.FailSessionRevocations); return inner.RevokeAsync(tokenHash, now, cancellationToken); }
    }
}

/// <summary>Posouvá čas serveru bez čekání, aby šlo ověřit přesné hranice platnosti session.</summary>
internal sealed class TestClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan duration) => now += duration;
}
