using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Pymen.Api.Endpoints;
using Pymen.Api.Middleware;
using Pymen.Application;
using Pymen.Infrastructure;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Persistence.Seed;
using Scalar.AspNetCore;

DotEnv.LoadFromAncestorDirectory(Directory.GetCurrentDirectory());
DotEnv.LoadFromAncestorDirectory(AppContext.BaseDirectory);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PymenDbContext>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("EnableOpenApi", false))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Configuration.GetValue("SkipSeed", false))
{
    await DemoDataSeeder.SeedAsync(app.Services);
    await DemoUserSeeder.SeedAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => !check.Tags.Contains("ready")
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapProductEndpoints();
app.MapWorkOrderEndpoints();
app.MapDeliveryNoteEndpoints();

app.Run();

public partial class Program;
