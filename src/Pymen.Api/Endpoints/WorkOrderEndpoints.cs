using Pymen.Api.Filters;
using Pymen.Application.Auth;
using Pymen.Application.Production;

namespace Pymen.Api.Endpoints;

public static class WorkOrderEndpoints
{
    public static RouteGroupBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-orders")
            .WithTags("WorkOrders");

        group.MapGet("/", async (WorkOrderService workOrders, CancellationToken ct) =>
                Results.Ok(await workOrders.ListAsync(ct)))
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapGet("/{id:guid}", async (Guid id, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var workOrder = await workOrders.GetAsync(id, ct);
                return workOrder is null ? Results.NotFound() : Results.Ok(workOrder);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/", async (CreateWorkOrderRequest request, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var created = await workOrders.CreateAsync(request, ct);
                return Results.Created($"/api/work-orders/{created.Id}", created);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner))
            .AddEndpointFilter<ValidationFilter<CreateWorkOrderRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateWorkOrderRequest request,
                WorkOrderService workOrders,
                CancellationToken ct) =>
            {
                var updated = await workOrders.UpdateAsync(id, request, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner))
            .AddEndpointFilter<ValidationFilter<UpdateWorkOrderRequest>>();

        group.MapPost("/{id:guid}/start", async (Guid id, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var updated = await workOrders.StartAsync(id, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/deliver", async (Guid id, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var updated = await workOrders.DeliverAsync(id, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/invoice", async (Guid id, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var updated = await workOrders.InvoiceAsync(id, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/cancel", async (Guid id, WorkOrderService workOrders, CancellationToken ct) =>
            {
                var updated = await workOrders.CancelAsync(id, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/start", async (
                Guid workOrderId,
                Guid operationId,
                WorkOrderService workOrders,
                CancellationToken ct) =>
            {
                var updated = await workOrders.StartOperationAsync(workOrderId, operationId, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/finish", async (
                Guid workOrderId,
                Guid operationId,
                WorkOrderService workOrders,
                CancellationToken ct) =>
            {
                var updated = await workOrders.FinishOperationAsync(workOrderId, operationId, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/cancel", async (
                Guid workOrderId,
                Guid operationId,
                WorkOrderService workOrders,
                CancellationToken ct) =>
            {
                var updated = await workOrders.CancelOperationAsync(workOrderId, operationId, ct);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        return group;
    }
}
