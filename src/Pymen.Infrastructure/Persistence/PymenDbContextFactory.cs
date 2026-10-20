using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Infrastructure.Persistence;

public sealed class PymenDbContextFactory : IDesignTimeDbContextFactory<PymenDbContext>
{
    public PymenDbContext CreateDbContext(string[] args)
    {
        DotEnv.LoadFromAncestorDirectory(Directory.GetCurrentDirectory());
        DotEnv.LoadFromAncestorDirectory(AppContext.BaseDirectory);

        var options = new DbContextOptionsBuilder<PymenDbContext>()
            .UseSqlServer(DotEnv.BuildLocalSqlConnectionString("Pymen"))
            .Options;

        return new PymenDbContext(options, new AmbientTenantContext());
    }
}
