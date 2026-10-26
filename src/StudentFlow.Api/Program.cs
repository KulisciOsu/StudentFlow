using StudentFlow.Api.Services;
using StudentFlow.Core.Interfaces;
using StudentFlow.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ProjectService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
