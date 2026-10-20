using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure.Persistence;

public sealed class PymenDbContextFactory : IDesignTimeDbContextFactory<PymenDbContext>
{
    public PymenDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PymenDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=Pymen;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False")
            .Options;

        return new PymenDbContext(options, new AmbientTenantContext());
    }
}
