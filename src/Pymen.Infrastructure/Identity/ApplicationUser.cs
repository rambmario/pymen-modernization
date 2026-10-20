using Microsoft.AspNetCore.Identity;

namespace Pymen.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
}
