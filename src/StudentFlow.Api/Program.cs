using Npgsql;
using StudentFlow.Core.Authentication;
using StudentFlow.Data.Authentication;
using StudentFlow.Core.Dashboard;
using StudentFlow.Data.Dashboard;
using StudentFlow.Api.Authentication;
using StudentFlow.Api.Development;

using StudentFlow.Core.Kanban;
using StudentFlow.Data.Kanban;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthSecurity(builder.Configuration);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection or use scripts/dev.mjs.");
builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
builder.Services.AddScoped<IUserAccountStore, PostgresUserAccountStore>();
builder.Services.AddScoped<IUserSessionStore, PostgresUserSessionStore>();
builder.Services.AddScoped<IDashboardRepository, PostgresDashboardRepository>();
builder.Services.AddScoped<IKanbanBoardRepository, KanbanBoardRepository>();

var app = builder.Build();

await DevelopmentAccountSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<AuthErrorMiddleware>();
app.UseStatusCodePages(async status =>
{
    if (status.HttpContext.Request.Path.StartsWithSegments("/api/auth") && status.HttpContext.Response.StatusCode == 415)
        await ApiProblems.WriteAsync(status.HttpContext, 415, "UNSUPPORTED_MEDIA_TYPE", "Nepodporovaný formát", "Pošlete požadavek ve formátu application/json.");
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program { }
