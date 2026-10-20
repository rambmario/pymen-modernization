using Microsoft.EntityFrameworkCore;
using Pymen.Application.Abstractions;
using Pymen.Domain.Production;

namespace Pymen.Application.Production;

public sealed class WorkOrderService
{
    private readonly IPymenDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IWorkOrderNumberAllocator _allocator;
    private readonly TimeProvider _time;

    public WorkOrderService(
        IPymenDbContext db,
        ITenantContext tenant,
        IWorkOrderNumberAllocator allocator,
        TimeProvider time)
    {
        _db = db;
        _tenant = tenant;
        _allocator = allocator;
        _time = time;
    }

    public async Task<IReadOnlyList<WorkOrderResponse>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await Query().AsNoTracking().OrderByDescending(w => w.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(w => w.ToResponse()).ToList();
    }

    public async Task<WorkOrderResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workOrder = await FindWorkOrderAsync(id, tracking: false, cancellationToken);
        return workOrder?.ToResponse();
    }

    public async Task<WorkOrderResponse> CreateAsync(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await _allocator.CreateWorkOrderAsync(
            _tenant.TenantId,
            request.CustomerId,
            request.Notes,
            request.Lines.Select(l => (l.ProductId, l.Quantity)).ToList(),
            request.Operations?.Select(o => o.OperationTypeId).ToList(),
            cancellationToken);

        var created = await FindWorkOrderAsync(workOrder.Id, tracking: false, cancellationToken)
            ?? throw new InvalidOperationException("Work order was created but could not be reloaded.");
        return created.ToResponse();
    }

    public async Task<WorkOrderResponse?> UpdateAsync(
        Guid id,
        UpdateWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var workOrder = await FindWorkOrderAsync(id, tracking: true, cancellationToken);
        if (workOrder is null)
        {
            return null;
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

        await _db.SaveChangesAsync(cancellationToken);
        return workOrder.ToResponse();
    }

    public Task<WorkOrderResponse?> StartAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(id, wo => wo.StartProduction(_time), cancellationToken);

    public Task<WorkOrderResponse?> DeliverAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(id, wo => wo.MarkDelivered(_time), cancellationToken);

    public Task<WorkOrderResponse?> InvoiceAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(id, wo => wo.MarkInvoiced(_time), cancellationToken);

    public Task<WorkOrderResponse?> CancelAsync(Guid id, CancellationToken cancellationToken = default) =>
        MutateAsync(id, wo => wo.Cancel(_time), cancellationToken);

    public async Task<WorkOrderResponse?> StartOperationAsync(
        Guid workOrderId,
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        await MutateOperationAsync(workOrderId, operationId, op => op.Start(_time), cancellationToken);

    public async Task<WorkOrderResponse?> FinishOperationAsync(
        Guid workOrderId,
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        await MutateOperationAsync(workOrderId, operationId, op => op.Finish(_time), cancellationToken);

    public async Task<WorkOrderResponse?> CancelOperationAsync(
        Guid workOrderId,
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        await MutateOperationAsync(workOrderId, operationId, op => op.Cancel(), cancellationToken);

    /// <summary>
    /// Single lookup used by all OT mutations: tracked graph with lines/operations, or null → 404.
    /// </summary>
    internal async Task<WorkOrder?> FindWorkOrderAsync(
        Guid id,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = Query();
        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    private async Task<WorkOrderResponse?> MutateAsync(
        Guid id,
        Action<WorkOrder> mutate,
        CancellationToken cancellationToken)
    {
        var workOrder = await FindWorkOrderAsync(id, tracking: true, cancellationToken);
        if (workOrder is null)
        {
            return null;
        }

        mutate(workOrder);
        await _db.SaveChangesAsync(cancellationToken);
        return workOrder.ToResponse();
    }

    private async Task<WorkOrderResponse?> MutateOperationAsync(
        Guid workOrderId,
        Guid operationId,
        Action<WorkOrderOperation> mutate,
        CancellationToken cancellationToken)
    {
        var workOrder = await FindWorkOrderAsync(workOrderId, tracking: true, cancellationToken);
        if (workOrder is null)
        {
            return null;
        }

        var operation = workOrder.Operations.FirstOrDefault(o => o.Id == operationId);
        if (operation is null)
        {
            return null;
        }

        mutate(operation);
        await _db.SaveChangesAsync(cancellationToken);
        return workOrder.ToResponse();
    }

    private IQueryable<WorkOrder> Query() =>
        _db.WorkOrders.Include(w => w.Lines).Include(w => w.Operations);
}
