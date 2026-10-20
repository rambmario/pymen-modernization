using Microsoft.EntityFrameworkCore;
using Pymen.Api.Filters;
using Pymen.Application.Abstractions;
using Pymen.Application.Auth;
using Pymen.Application.Production;
using Pymen.Domain.Production;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.Endpoints;

public static class DeliveryNoteEndpoints
{
    public static RouteGroupBuilder MapDeliveryNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-notes")
            .WithTags("DeliveryNotes")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (PymenDbContext db, CancellationToken ct) =>
        {
            var items = await db.DeliveryNotes
                .Include(d => d.Lines)
                .AsNoTracking()
                .OrderByDescending(d => d.DeliveredAt)
                .ToListAsync(ct);
            return Results.Ok(items.Select(d => d.ToResponse()));
        });

        group.MapGet("/{id:guid}", async (Guid id, PymenDbContext db, CancellationToken ct) =>
        {
            var note = await db.DeliveryNotes
                .Include(d => d.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            return note is null ? Results.NotFound() : Results.Ok(note.ToResponse());
        });

        group.MapPost("/", async (
                CreateDeliveryNoteRequest request,
                PymenDbContext db,
                ITenantContext tenant,
                TimeProvider time,
                CancellationToken ct) =>
            {
                var workOrderExists = await db.WorkOrders.AnyAsync(w => w.Id == request.WorkOrderId, ct);
                if (!workOrderExists)
                {
                    return Results.NotFound();
                }

                var note = DeliveryNote.Create(
                    tenant.TenantId,
                    request.WorkOrderId,
                    request.Number,
                    time,
                    request.Notes);

                foreach (var line in request.Lines)
                {
                    note.AddLine(line.ProductId, line.Quantity);
                }

                db.DeliveryNotes.Add(note);
                await db.SaveChangesAsync(ct);

                var created = await db.DeliveryNotes
                    .Include(d => d.Lines)
                    .AsNoTracking()
                    .FirstAsync(d => d.Id == note.Id, ct);

                return Results.Created($"/api/delivery-notes/{created.Id}", created.ToResponse());
            })
            .AddEndpointFilter<ValidationFilter<CreateDeliveryNoteRequest>>();

        return group;
    }
}
