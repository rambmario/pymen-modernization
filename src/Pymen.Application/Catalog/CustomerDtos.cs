namespace Pymen.Application.Catalog;

public sealed record CustomerResponse(Guid Id, string Name, string? TaxId, bool IsActive);

public sealed record CreateCustomerRequest(string Name, string? TaxId);

public sealed record UpdateCustomerRequest(string Name, string? TaxId, bool IsActive);
