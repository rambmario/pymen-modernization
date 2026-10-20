using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Auth;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.IntegrationTests;

public sealed class AuthHardeningTests
{
    [Fact]
    public async Task Host_fails_to_start_without_jwt_signing_key()
    {
        TestEnvironment.EnsureLoaded();
        var previous = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
        try
        {
            // Non-empty so DotEnv does not refill from .env; too short to pass ValidateOnStart.
            Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "short");

            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("SkipSeed", "true");
                builder.UseSetting("Seed:Enabled", "false");
                builder.UseSetting("Jwt:SigningKey", "short");
                builder.UseSetting("ConnectionStrings:Pymen",
                    DotEnv.BuildLocalSqlConnectionString("Pymen_AuthHardening", connectTimeoutSeconds: 1));
            });

            Exception? caught = null;
            try
            {
                var client = factory.CreateClient();
                await client.GetAsync("/health");
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            caught.Should().NotBeNull("host must fail when Jwt:SigningKey is too short");
            caught!.ToString().Should().Contain("SigningKey");
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", previous);
        }
    }

    [Fact]
    public async Task Probe_endpoint_without_auth_attributes_returns_401()
    {
        TestEnvironment.EnsureLoaded();
        Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", "TEST_ONLY_signing_key_32chars_min!!");

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SkipSeed", "true");
            builder.UseSetting("Seed:Enabled", "false");
            builder.UseSetting("EnableAuthProbe", "true");
            builder.UseSetting("Jwt:SigningKey", "TEST_ONLY_signing_key_32chars_min!!");
            builder.UseSetting("ConnectionStrings:Pymen",
                DotEnv.BuildLocalSqlConnectionString("Pymen_AuthProbe", connectTimeoutSeconds: 1));
        });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/__probe/auth-required");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

[Collection(SqlServerCollection.Name)]
public sealed class AuthRateLimitTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private PymenApiFactory? _factory;
    private HttpClient? _client;

    public AuthRateLimitTests(SqlServerFixture sql)
    {
        _sql = sql;
    }

    public async Task InitializeAsync()
    {
        if (!_sql.IsAvailable)
        {
            return;
        }

        _factory = new PymenApiFactory(_sql);
        _client = _factory.CreateClient();
        await _client.GetAsync("/health");
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Login_rate_limit_returns_429()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        HttpResponseMessage? last = null;
        for (var i = 0; i < 8; i++)
        {
            last = await _client!.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest("nobody@demo.local", "WrongPass123"));
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
