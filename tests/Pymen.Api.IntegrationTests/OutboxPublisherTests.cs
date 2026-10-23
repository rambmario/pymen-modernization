using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Pymen.Api.IntegrationTests.Infrastructure;
using Pymen.Domain.Events;
using Pymen.Infrastructure.Messaging;
using Pymen.Infrastructure.Persistence;
using Pymen.Infrastructure.Tenancy;

namespace Pymen.Api.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public sealed class OutboxPublisherTests : IAsyncLifetime
{
    private readonly SqlServerFixture _sql;
    private PymenApiFactory? _factory;

    public OutboxPublisherTests(SqlServerFixture sql)
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
        var client = _factory.CreateClient();
        await client.GetAsync("/health");
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task PublishPending_does_not_publish_the_same_MessageId_twice()
    {
        Skip.If(!_sql.IsAvailable || _factory is null, "Docker is not available for Testcontainers SQL Server.");

        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-22T21:00:00Z"));
        var eventPayload = new WorkOrderDelivered(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "000100000099",
            time.GetUtcNow());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
            var tenantContext = scope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            tenantContext.Clear();

            db.OutboxMessages.Add(OutboxMessage.FromDomainEvent(
                eventPayload,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
            await db.SaveChangesAsync();
        }

        var fakeSender = new RecordingOutboxSender();
        var publisher = new OutboxPublisher(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            fakeSender,
            time,
            Options.Create(new OutboxOptions { BatchSize = 100 }),
            NullLogger<OutboxPublisher>.Instance);

        var first = await publisher.PublishPendingAsync();
        var second = await publisher.PublishPendingAsync();

        first.Should().BeGreaterThan(0);
        second.Should().Be(0);
        fakeSender.SentMessageIds.Should().OnlyHaveUniqueItems();
        fakeSender.SentMessageIds.Count.Should().Be(first);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var published = await verifyDb.OutboxMessages
            .CountAsync(m => m.Status == OutboxMessageStatus.Published);
        published.Should().BeGreaterThanOrEqualTo(first);
    }

    private sealed class RecordingOutboxSender : IOutboxMessageSender
    {
        public List<Guid> SentMessageIds { get; } = [];

        public Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            SentMessageIds.Add(message.Id);
            return Task.CompletedTask;
        }
    }
}
