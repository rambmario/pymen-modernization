using Microsoft.Extensions.DependencyInjection;

namespace Pymen.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
