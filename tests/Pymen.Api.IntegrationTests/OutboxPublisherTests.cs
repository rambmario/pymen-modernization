using System.Collections.Concurrent;
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
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

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

        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-24T12:00:00Z"));
        var ids = await SeedPendingMessagesAsync(time, count: 1);

        var fakeSender = new RecordingOutboxSender();
        var publisher = CreatePublisher(fakeSender, time, "seq-a");

        var first = await publisher.PublishPendingAsync();
        var second = await publisher.PublishPendingAsync();

        first.Should().BeGreaterThan(0);
        second.Should().Be(0);
        fakeSender.SentMessageIds.Should().OnlyHaveUniqueItems();
        fakeSender.SentMessageIds.Should().Contain(ids[0]);
    }

    [SkippableFact]
    public async Task Two_parallel_publishers_send_each_message_only_once()
    {
        Skip.If(!_sql.IsAvailable || _factory is null, "Docker is not available for Testcontainers SQL Server.");

        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-24T12:10:00Z"));
        var ids = await SeedPendingMessagesAsync(time, count: 20);

        var sharedSender = new RecordingOutboxSender(sendDelay: TimeSpan.FromMilliseconds(25));
        var options = Options.Create(new OutboxOptions
        {
            BatchSize = 10,
            MaxAttempts = 5,
            LockDurationSeconds = 60
        });

        var publisherA = new OutboxPublisher(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            sharedSender,
            time,
            options,
            NullLogger<OutboxPublisher>.Instance,
            instanceId: "replica-a");

        var publisherB = new OutboxPublisher(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            sharedSender,
            time,
            options,
            NullLogger<OutboxPublisher>.Instance,
            instanceId: "replica-b");

        var results = await Task.WhenAll(
            publisherA.PublishPendingAsync(),
            publisherB.PublishPendingAsync());

        // Drain any leftovers (batch size limited).
        await publisherA.PublishPendingAsync();
        await publisherB.PublishPendingAsync();

        results.Sum().Should().BeGreaterThan(0);
        sharedSender.SentMessageIds.Should().OnlyHaveUniqueItems();
        foreach (var id in ids)
        {
            sharedSender.SentMessageIds.Count(x => x == id).Should().Be(1, "message {0} sent once", id);
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var published = await db.OutboxMessages.CountAsync(m => ids.Contains(m.Id) && m.Status == OutboxMessageStatus.Published);
        published.Should().Be(ids.Count);
    }

    [SkippableFact]
    public async Task Failed_sends_use_backoff_then_mark_Failed_after_max_attempts()
    {
        Skip.If(!_sql.IsAvailable || _factory is null, "Docker is not available for Testcontainers SQL Server.");

        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-24T12:20:00Z"));
        var ids = await SeedPendingMessagesAsync(time, count: 1);
        var messageId = ids[0];

        var failingSender = new FailingOutboxSender();
        var options = Options.Create(new OutboxOptions
        {
            BatchSize = 10,
            MaxAttempts = 3,
            LockDurationSeconds = 30,
            MaxBackoffSeconds = 300
        });
        var publisher = new OutboxPublisher(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            failingSender,
            time,
            options,
            NullLogger<OutboxPublisher>.Instance,
            instanceId: "retry-test");

        (await publisher.PublishPendingAsync()).Should().Be(0);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
            var msg = await db.OutboxMessages.SingleAsync(m => m.Id == messageId);
            msg.AttemptCount.Should().Be(1);
            msg.Status.Should().Be(OutboxMessageStatus.Pending);
            msg.NextAttemptAt.Should().Be(time.GetUtcNow().AddSeconds(2));
            msg.LockedUntil.Should().BeNull();
        }

        // Too early for backoff — should not claim.
        (await publisher.PublishPendingAsync()).Should().Be(0);

        time.Advance(TimeSpan.FromSeconds(2));
        (await publisher.PublishPendingAsync()).Should().Be(0);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
            var msg = await db.OutboxMessages.SingleAsync(m => m.Id == messageId);
            msg.AttemptCount.Should().Be(2);
            msg.NextAttemptAt.Should().Be(time.GetUtcNow().AddSeconds(4));
        }

        time.Advance(TimeSpan.FromSeconds(4));
        (await publisher.PublishPendingAsync()).Should().Be(0);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
            var msg = await db.OutboxMessages.SingleAsync(m => m.Id == messageId);
            msg.AttemptCount.Should().Be(3);
            msg.Status.Should().Be(OutboxMessageStatus.Failed);
            msg.NextAttemptAt.Should().BeNull();
            msg.LastError.Should().NotBeNullOrWhiteSpace();
        }

        // Terminal — no more claims.
        time.Advance(TimeSpan.FromMinutes(10));
        (await publisher.PublishPendingAsync()).Should().Be(0);
        failingSender.Attempts.Should().Be(3);
    }

    private OutboxPublisher CreatePublisher(IOutboxMessageSender sender, TimeProvider time, string instanceId) =>
        new(
            _factory!.Services.GetRequiredService<IServiceScopeFactory>(),
            sender,
            time,
            Options.Create(new OutboxOptions { BatchSize = 100, MaxAttempts = 5 }),
            NullLogger<OutboxPublisher>.Instance,
            instanceId);

    private async Task<IReadOnlyList<Guid>> SeedPendingMessagesAsync(FakeTimeProvider time, int count)
    {
        var ids = new List<Guid>();
        await using var scope = _factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PymenDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
        tenantContext.Clear();

        for (var i = 0; i < count; i++)
        {
            var evt = new WorkOrderDelivered(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                $"00010000{i:D4}",
                time.GetUtcNow());
            var msg = OutboxMessage.FromDomainEvent(evt, JsonOptions);
            db.OutboxMessages.Add(msg);
            ids.Add(msg.Id);
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private sealed class RecordingOutboxSender(TimeSpan? sendDelay = null) : IOutboxMessageSender
    {
        private readonly ConcurrentDictionary<Guid, byte> _sent = new();

        public IReadOnlyCollection<Guid> SentMessageIds => _sent.Keys.ToArray();

        public async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            if (sendDelay is { } delay)
            {
                await Task.Delay(delay, cancellationToken);
            }

            if (!_sent.TryAdd(message.Id, 0))
            {
                throw new InvalidOperationException($"Duplicate send for {message.Id}");
            }
        }
    }

    private sealed class FailingOutboxSender : IOutboxMessageSender
    {
        public int Attempts { get; private set; }

        public Task SendAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("Simulated Service Bus failure");
        }
    }
}
