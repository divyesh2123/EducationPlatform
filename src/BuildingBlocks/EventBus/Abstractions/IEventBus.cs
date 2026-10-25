namespace EventBus.Abstractions;

public interface IEventBus
{
    void Publish(IntegrationEvent @event);
}
