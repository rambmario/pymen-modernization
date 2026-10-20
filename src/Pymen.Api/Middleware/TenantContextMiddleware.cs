using System.Security.Claims;
using Pymen.Application.Auth;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Api.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AmbientTenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var raw = context.User.FindFirstValue(TenantClaimTypes.TenantId)
                      ?? context.User.FindFirstValue("tenant_id");

            if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var tenantId) || tenantId == Guid.Empty)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Unauthorized",
                    detail = "Authentication token is missing a valid tenant_id claim.",
                    status = StatusCodes.Status401Unauthorized
                });
                return;
            }

            tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}
