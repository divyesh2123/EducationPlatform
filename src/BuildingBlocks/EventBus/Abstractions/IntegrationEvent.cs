namespace EventBus.Abstractions;

/// <summary>
/// Base type for every message that travels between microservices over RabbitMQ.
/// The concrete type name is used as the RabbitMQ routing key.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime CreationDate { get; init; } = DateTime.UtcNow;
}
