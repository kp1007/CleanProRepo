namespace CleanPro.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId, string Name, decimal Price) : DomainEvent;
