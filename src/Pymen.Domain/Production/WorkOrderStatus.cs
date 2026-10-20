namespace Pymen.Domain.Production;

public enum WorkOrderStatus
{
    Draft = 0,
    InProduction = 1,
    Delivered = 2,
    Invoiced = 3,
    Cancelled = 4
}
