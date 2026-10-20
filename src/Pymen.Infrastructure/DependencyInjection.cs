using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Application.Abstractions;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AmbientTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());
        services.AddScoped<IWorkOrderNumberAllocator, WorkOrderNumberAllocator>();

        var connectionString = ResolveConnectionString(configuration);
        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }

    internal static string ResolveConnectionString(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString("Pymen");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DotEnv.BuildLocalSqlConnectionString("Pymen");
        }

        if (configured.Contains("{MSSQL_SA_PASSWORD}", StringComparison.Ordinal))
        {
            return configured.Replace("{MSSQL_SA_PASSWORD}", DotEnv.RequireSqlPassword(), StringComparison.Ordinal);
        }

        return configured;
    }
}
