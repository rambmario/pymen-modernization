using Pymen.Application.Auth;
using Pymen.Application.Catalog;

namespace Pymen.Api.Endpoints;

public static class OperationTypeEndpoints
{
    public static RouteGroupBuilder MapOperationTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operation-types")
            .WithTags("OperationTypes")
            .RequireAuthorization(policy =>
                policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapGet("/", async (CatalogService catalog, CancellationToken ct) =>
            Results.Ok(await catalog.ListOperationTypesAsync(ct)));

        return group;
    }
}
