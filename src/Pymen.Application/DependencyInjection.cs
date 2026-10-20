using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Application.Catalog;
using Pymen.Application.Production;

namespace Pymen.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<CatalogService>();
        services.AddScoped<WorkOrderService>();
        services.AddScoped<DeliveryNoteService>();
        return services;
    }
}
