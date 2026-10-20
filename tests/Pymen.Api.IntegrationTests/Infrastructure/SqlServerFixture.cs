using DotNet.Testcontainers.Builders;
using Testcontainers.MsSql;

namespace Pymen.Api.IntegrationTests.Infrastructure;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _sql;

    public string ConnectionString =>
        _sql?.GetConnectionString()
        ?? throw new InvalidOperationException("SQL Server container was not started.");

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        TestEnvironment.EnsureLoaded();
        var password = TestEnvironment.SqlPassword;

        try
        {
            _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword(password)
                .Build();
            await _sql.StartAsync();
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is DockerUnavailableException || ex is AggregateException)
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_sql is not null)
        {
            await _sql.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sql-server";
}
