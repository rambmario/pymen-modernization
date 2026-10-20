using Pymen.Api.Filters;
using Pymen.Application.Auth;
using Pymen.Application.Production;

namespace Pymen.Api.Endpoints;

public static class DeliveryNoteEndpoints
{
    public static RouteGroupBuilder MapDeliveryNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/delivery-notes")
            .WithTags("DeliveryNotes")
            .RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin, AppRoles.Planner));

        group.MapGet("/", async (DeliveryNoteService notes, CancellationToken ct) =>
            Results.Ok(await notes.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, DeliveryNoteService notes, CancellationToken ct) =>
        {
            var note = await notes.GetAsync(id, ct);
            return note is null ? Results.NotFound() : Results.Ok(note);
        });

        group.MapPost("/", async (CreateDeliveryNoteRequest request, DeliveryNoteService notes, CancellationToken ct) =>
            {
                var created = await notes.CreateAsync(request, ct);
                return created is null
                    ? Results.NotFound()
                    : Results.Created($"/api/delivery-notes/{created.Id}", created);
            })
            .AddEndpointFilter<ValidationFilter<CreateDeliveryNoteRequest>>();

        return group;
    }
}
