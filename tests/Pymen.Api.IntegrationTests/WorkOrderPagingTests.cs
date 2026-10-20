using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Application.Auth;
using Pymen.Application.Catalog;
using Pymen.Application.Common;
using Pymen.Application.Production;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class WorkOrderPagingTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly SqlServerFixture _sql;
    private PymenApiFactory? _factory;
    private HttpClient? _client;

    public WorkOrderPagingTests(SqlServerFixture sql)
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
    public async Task List_supports_status_filter_and_server_paging()
    {
        Skip.If(!_sql.IsAvailable || _client is null, "Docker is not available for Testcontainers SQL Server.");

        var login = await LoginAsync("planner.t1@demo.local", "PlannerPass123");
        Authorize(login.AccessToken);

        var customers = await _client!.GetFromJsonAsync<List<CustomerResponse>>("/api/customers", JsonOptions);
        var products = await _client.GetFromJsonAsync<List<ProductResponse>>("/api/products", JsonOptions);
        var finished = products!.First(p => !p.IsRawMaterial);
        var customerId = customers![0].Id;

        for (var i = 0; i < 3; i++)
        {
            var create = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(
                customerId,
                $"paging-{i}",
                [new WorkOrderLineRequest(finished.Id, 1)],
                null));
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = await create.Content.ReadFromJsonAsync<WorkOrderResponse>(JsonOptions);
            if (i == 0)
            {
                var started = await _client.PostAsync($"/api/work-orders/{created!.Id}/start", null);
                started.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        var draftPage1 = await _client.GetFromJsonAsync<PagedResult<WorkOrderResponse>>(
            "/api/work-orders?status=Draft&page=1&pageSize=1",
            JsonOptions);

        draftPage1.Should().NotBeNull();
        draftPage1!.Page.Should().Be(1);
        draftPage1.PageSize.Should().Be(1);
        draftPage1.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        draftPage1.Items.Should().HaveCount(1);
        draftPage1.Items[0].Status.Should().Be("Draft");

        var draftPage2 = await _client.GetFromJsonAsync<PagedResult<WorkOrderResponse>>(
            "/api/work-orders?status=Draft&page=2&pageSize=1",
            JsonOptions);

        draftPage2!.Items.Should().HaveCount(1);
        draftPage2.Items[0].Id.Should().NotBe(draftPage1.Items[0].Id);
        draftPage2.TotalCount.Should().Be(draftPage1.TotalCount);

        var inProduction = await _client.GetFromJsonAsync<PagedResult<WorkOrderResponse>>(
            "/api/work-orders?status=InProduction&pageSize=50",
            JsonOptions);
        inProduction!.Items.Should().OnlyContain(w => w.Status == "InProduction");
        inProduction.TotalCount.Should().Be(inProduction.Items.Count);
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
