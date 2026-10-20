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

        services.AddDbContext<PymenDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Pymen")));

        return services;
    }
}
