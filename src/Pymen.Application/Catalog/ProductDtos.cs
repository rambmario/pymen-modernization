namespace Pymen.Application.Catalog;

public sealed record ProductResponse(Guid Id, string Sku, string Name, bool IsRawMaterial, bool IsActive);

public sealed record CreateProductRequest(string Sku, string Name, bool IsRawMaterial);

public sealed record UpdateProductRequest(string Name, bool IsActive);
