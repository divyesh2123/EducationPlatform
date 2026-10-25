namespace EventBus.Abstractions;

/// <summary>
/// Maps routing key (event type name) to the CLR type, filled by AddSubscription&lt;TEvent, THandler&gt;().
/// </summary>
public class EventBusSubscriptionInfo
{
    public Dictionary<string, Type> EventTypes { get; } = new();
}
