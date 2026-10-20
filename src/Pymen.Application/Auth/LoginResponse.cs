namespace Pymen.Application.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid TenantId,
    string Email,
    IReadOnlyList<string> Roles);
