using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;
using Pymen.Application.Production;
using Pymen.Domain.Events;
using Pymen.Infrastructure.Messaging;
using Pymen.Infrastructure.Persistence;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class OutboxPersistenceTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SqlServerFixture _sql;
    private PymenApiFactory? _factory;
    private HttpClient? _client;

    public OutboxPersistenceTests(SqlServerFixture sql)
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
    public async Task Deliver_persists_WorkOrderDelivered_in_Outbox_same_SaveChanges()
    {
        Skip.If(!_sql.IsAvailable || _client is null || _factory is null, "Docker is not available for Testcontainers SQL Server.");

        var login = await LoginAsync("planner.t1@demo.local", "PlannerPass123");
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var customers = await _client.GetFromJsonAsync<List<CustomerResponse>>("/api/customers", JsonOptions);
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/api/products", JsonOptions);
        var finished = products!.First(p => !p.IsRawMaterial);

        var createResponse = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(
            customers![0].Id,
            "Outbox OT",
            [new WorkOrderLineRequest(finished.Id, 1)],
            null));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var workOrder = await createResponse.Content.ReadFromJsonAsync<WorkOrderResponse>(JsonOptions);

        (await _client.PostAsync($"/api/work-orders/{workOrder!.Id}/start", null)).EnsureSuccessStatusCode();
        (await _client.PostAsync($"/api/work-orders/{workOrder.Id}/deliver", null)).EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var eventType = typeof(WorkOrderDelivered).FullName!;

        var outbox = await db.OutboxMessages
            .Where(m => m.Type == eventType && m.Payload.Contains(workOrder.Id.ToString()))
            .ToListAsync();

        outbox.Should().ContainSingle();
        outbox[0].Status.Should().Be(OutboxMessageStatus.Pending);
        outbox[0].TenantId.Should().NotBeNull();
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _client!.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
    }
}
