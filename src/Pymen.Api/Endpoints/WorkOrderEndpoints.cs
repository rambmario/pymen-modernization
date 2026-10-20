using Microsoft.EntityFrameworkCore;
using Pymen.Api.Filters;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Application.Production;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.Endpoints;

public static class WorkOrderEndpoints
{
    public static RouteGroupBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-orders")
            .WithTags("WorkOrders");

        group.MapGet("/", async (PymenDbContext db, CancellationToken ct) =>
            {
                var items = await Query(db).AsNoTracking().OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
                return Results.Ok(items.Select(w => w.ToResponse()));
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapGet("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
            {
                var workOrder = await Query(db).AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);
                return workOrder is null ? Results.NotFound() : Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/", async (
                CreateWorkOrderRequest request,
                IWorkOrderNumberAllocator allocator,
                PymenDbContext db,
                ITenantContext tenant,
                CancellationToken ct) =>
            {
                var workOrder = await allocator.CreateWorkOrderAsync(
                    tenant.TenantId,
                    request.CustomerId,
                    request.Notes,
                    request.Lines.Select(l => (l.ProductId, l.Quantity)).ToList(),
                    request.Operations?.Select(o => o.OperationTypeId).ToList(),
                    ct);

                var created = await Query(db).AsNoTracking().FirstAsync(w => w.Id == workOrder.Id, ct);
                return Results.Created($"/api/work-orders/{created.Id}", created.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner))
            .AddEndpointFilter<ValidationFilter<CreateWorkOrderRequest>>();

        group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateWorkOrderRequest request,
                PymenDbContext db,
                CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                if (request.Notes is not null)
                {
                    workOrder.UpdateNotes(request.Notes);
                }

                if (request.LinesToAdd is not null)
                {
                    foreach (var line in request.LinesToAdd)
                    {
                        workOrder.AddLine(line.ProductId, line.Quantity);
                    }
                }

                if (request.OperationsToAdd is not null)
                {
                    foreach (var operation in request.OperationsToAdd)
                    {
                        workOrder.AddOperation(operation.OperationTypeId);
                    }
                }

                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner))
            .AddEndpointFilter<ValidationFilter<UpdateWorkOrderRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, PymenDbContext db, TimeProvider time, CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                workOrder.Cancel(time);
                await db.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/start", async (Guid id, PymenDbContext db, TimeProvider time, CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                workOrder.StartProduction(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/deliver", async (Guid id, PymenDbContext db, TimeProvider time, CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                workOrder.MarkDelivered(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/invoice", async (Guid id, PymenDbContext db, TimeProvider time, CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                workOrder.MarkInvoiced(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{id:guid}/cancel", async (Guid id, PymenDbContext db, TimeProvider time, CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == id, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                workOrder.Cancel(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/start", async (
                Guid workOrderId,
                Guid operationId,
                PymenDbContext db,
                TimeProvider time,
                CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == workOrderId, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                var operation = workOrder.Operations.FirstOrDefault(o => o.Id == operationId);
                if (operation is null)
                {
                    return Results.NotFound();
                }

                operation.Start(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/finish", async (
                Guid workOrderId,
                Guid operationId,
                PymenDbContext db,
                TimeProvider time,
                CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == workOrderId, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                var operation = workOrder.Operations.FirstOrDefault(o => o.Id == operationId);
                if (operation is null)
                {
                    return Results.NotFound();
                }

                operation.Finish(time);
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        group.MapPost("/{workOrderId:guid}/operations/{operationId:guid}/cancel", async (
                Guid workOrderId,
                Guid operationId,
                PymenDbContext db,
                CancellationToken ct) =>
            {
                var workOrder = await Query(db).FirstOrDefaultAsync(w => w.Id == workOrderId, ct);
                if (workOrder is null)
                {
                    return Results.NotFound();
                }

                var operation = workOrder.Operations.FirstOrDefault(o => o.Id == operationId);
                if (operation is null)
                {
                    return Results.NotFound();
                }

                operation.Cancel();
                await db.SaveChangesAsync(ct);
                return Results.Ok(workOrder.ToResponse());
            })
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner, AppRoles.Operator));

        return group;
    }

    private static IQueryable<Domain.Production.WorkOrder> Query(PymenDbContext db) =>
        db.WorkOrders.Include(w => w.Lines).Include(w => w.Operations);
}
