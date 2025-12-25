namespace CleanPro.Domain.Events;

public sealed record ProductUpdatedEvent(Guid ProductId, string Name, decimal Price) : DomainEvent;
