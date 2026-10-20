using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pymen.Application.Auth;
using Pymen.Domain.Tenancy;
using Pymen.Infrastructure.Identity;

namespace Pymen.Infrastructure.Persistence.Seed;

public static class DemoUserSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUserSeeder");

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminPassword = RequireDemoPassword("DEMO_PASSWORD_ADMIN");
        var plannerPassword = RequireDemoPassword("DEMO_PASSWORD_PLANNER");
        var operatorPassword = RequireDemoPassword("DEMO_PASSWORD_OPERATOR");

        var tenants = await db.Tenants.AsNoTracking().OrderBy(t => t.PointOfSaleCode).ToListAsync(cancellationToken);
        if (tenants.Count == 0)
        {
            logger.LogWarning("No tenants found; skipping demo user seed.");
            return;
        }

        foreach (var tenant in tenants)
        {
            var slug = tenant.PointOfSaleCode.TrimStart('0');
            if (string.IsNullOrEmpty(slug))
            {
                slug = tenant.PointOfSaleCode;
            }

            await EnsureUserAsync(userManager, logger, tenant, $"admin.t{slug}@demo.local", AppRoles.Admin, adminPassword);
            await EnsureUserAsync(userManager, logger, tenant, $"planner.t{slug}@demo.local", AppRoles.Planner, plannerPassword);
            await EnsureUserAsync(userManager, logger, tenant, $"operator.t{slug}@demo.local", AppRoles.Operator, operatorPassword);
        }
    }

    private static string RequireDemoPassword(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{variableName} is not set. Copy .env.example to .env (or use user-secrets) and set demo passwords.");
        }

        return value;
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        Tenant tenant,
        string email,
        string role,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenant.Id
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create demo user '{email}': {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        logger.LogInformation("Seeded demo user {Email} ({Role}) for tenant {Tenant}.", email, role, tenant.Name);
    }
}
