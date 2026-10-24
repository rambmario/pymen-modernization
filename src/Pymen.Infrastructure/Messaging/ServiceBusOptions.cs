namespace Pymen.Infrastructure.Messaging;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string? ConnectionString { get; set; }
    public string QueueName { get; set; } = "domain-events";
}

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>When false, the BackgroundService publisher is not registered (tests / SQL-only).</summary>
    public bool PublisherEnabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 20;

    /// <summary>Max publish attempts before Status = Failed (default 5).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>How long a claimed row stays locked for a publisher replica.</summary>
    public int LockDurationSeconds { get; set; } = 30;

    /// <summary>Cap for exponential backoff delay (2^attempt seconds).</summary>
    public int MaxBackoffSeconds { get; set; } = 300;
}
