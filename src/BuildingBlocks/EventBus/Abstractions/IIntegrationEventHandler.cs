namespace EventBus.Abstractions;

/// <summary>
/// Non-generic handler so the event bus can invoke handlers without knowing the event type at compile time.
/// </summary>
public interface IIntegrationEventHandler
{
    Task Handle(IntegrationEvent @event);
}

public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event);

    Task IIntegrationEventHandler.Handle(IntegrationEvent @event) => Handle((TEvent)@event);
}
