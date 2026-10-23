namespace Pymen.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
