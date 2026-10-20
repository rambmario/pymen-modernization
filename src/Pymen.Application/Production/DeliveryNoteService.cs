using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Production;

namespace Pymen.Application.Production;

public sealed class DeliveryNoteService
{
    private readonly IPymenDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;

    public DeliveryNoteService(IPymenDbContext db, ITenantContext tenant, TimeProvider time)
    {
        _db = db;
        _tenant = tenant;
        _time = time;
    }

    public async Task<IReadOnlyList<DeliveryNoteResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.DeliveryNotes
            .Include(d => d.Lines)
            .AsNoTracking()
            .OrderByDescending(d => d.DeliveredAt)
            .ToListAsync(cancellationToken);
        return items.Select(d => d.ToResponse()).ToList();
    }

    public async Task<DeliveryNoteResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var note = await _db.DeliveryNotes
            .Include(d => d.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        return note?.ToResponse();
    }

    public async Task<DeliveryNoteResponse?> CreateAsync(
        CreateDeliveryNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var workOrderExists = await _db.WorkOrders.AnyAsync(w => w.Id == request.WorkOrderId, cancellationToken);
        if (!workOrderExists)
        {
            return null;
        }

        var note = DeliveryNote.Create(
            _tenant.TenantId,
            request.WorkOrderId,
            request.Number,
            _time,
            request.Notes);

        foreach (var line in request.Lines)
        {
            note.AddLine(line.ProductId, line.Quantity);
        }

        _db.DeliveryNotes.Add(note);
        await _db.SaveChangesAsync(cancellationToken);

        var created = await _db.DeliveryNotes
            .Include(d => d.Lines)
            .AsNoTracking()
            .FirstAsync(d => d.Id == note.Id, cancellationToken);

        return created.ToResponse();
    }
}
