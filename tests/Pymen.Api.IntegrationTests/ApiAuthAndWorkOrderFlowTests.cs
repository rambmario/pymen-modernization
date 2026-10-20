using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;
using Pymen.Application.Production;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class ApiAuthAndWorkOrderFlowTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SqlServerFixture _sql;
    private PymenApiFactory? _factory;
    private HttpClient? _client;

    public ApiAuthAndWorkOrderFlowTests(SqlServerFixture sql)
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
        // Force host start + seed
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
    public async Task Login_returns_jwt_with_tenant()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        var login = await LoginAsync("admin.t1@demo.local", "AdminPass123");
        login.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.TenantId.Should().NotBe(Guid.Empty);
        login.Roles.Should().Contain(AppRoles.Admin);
    }

    [SkippableFact]
    public async Task Planner_can_run_full_work_order_flow()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        var login = await LoginAsync("planner.t1@demo.local", "PlannerPass123");
        Authorize(login.AccessToken);

        var customers = await _client!.GetFromJsonAsync<List<CustomerResponse>>("/api/customers", JsonOptions);
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/api/products", JsonOptions);
        customers.Should().NotBeEmpty();
        products.Should().NotBeEmpty();

        var finished = products!.First(p => !p.IsRawMaterial);
        var customerId = customers![0].Id;

        // Operation types are not exposed via API yet — create WO without operations, then start/deliver/invoice.
        var createResponse = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(
            customerId,
            "Integration flow OT",
            [new WorkOrderLineRequest(finished.Id, 1)],
            null));

        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "body: {0}", createBody);
        var workOrder = JsonSerializer.Deserialize<WorkOrderResponse>(createBody, JsonOptions);
        workOrder.Should().NotBeNull();

        var started = await _client.PostAsync($"/api/work-orders/{workOrder!.Id}/start", null);
        started.StatusCode.Should().Be(HttpStatusCode.OK);

        var delivered = await _client.PostAsync($"/api/work-orders/{workOrder.Id}/deliver", null);
        delivered.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoiced = await _client.PostAsync($"/api/work-orders/{workOrder.Id}/invoice", null);
        invoiced.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalWo = await invoiced.Content.ReadFromJsonAsync<WorkOrderResponse>(JsonOptions);
        finalWo!.Status.Should().Be("Invoiced");
    }

    [SkippableFact]
    public async Task Tenant_A_requesting_tenant_B_work_order_returns_404()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        var adminB = await LoginAsync("admin.t2@demo.local", "AdminPass123");
        Authorize(adminB.AccessToken);
        var ordersB = await _client!.GetFromJsonAsync<List<WorkOrderResponse>>("/api/work-orders", JsonOptions);
        ordersB.Should().NotBeEmpty();
        var foreignId = ordersB![0].Id;

        var adminA = await LoginAsync("admin.t1@demo.local", "AdminPass123");
        Authorize(adminA.AccessToken);
        var response = await _client.GetAsync($"/api/work-orders/{foreignId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Operator_cancelling_work_order_returns_403()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        var planner = await LoginAsync("planner.t1@demo.local", "PlannerPass123");
        Authorize(planner.AccessToken);
        var orders = await _client!.GetFromJsonAsync<List<WorkOrderResponse>>("/api/work-orders", JsonOptions);
        var draft = orders!.First(o => o.Status == "Draft");

        var op = await LoginAsync("operator.t1@demo.local", "OperatorPass123");
        Authorize(op.AccessToken);
        var response = await _client.PostAsync($"/api/work-orders/{draft.Id}/cancel", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var response = await _client!.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        body.Should().NotBeNull();
        return body!;
    }

    private void Authorize(string token)
    {
        _client!.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
