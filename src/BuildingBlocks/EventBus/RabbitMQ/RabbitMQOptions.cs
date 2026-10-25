namespace EventBus.RabbitMQ;

public class RabbitMQOptions
{
    public const string SectionName = "RabbitMQ";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Direct exchange shared by all services. Routing key = event type name.</summary>
    public string ExchangeName { get; set; } = "education_event_bus";

    /// <summary>Queue owned by this service. Required only for services that consume events.</summary>
    public string QueueName { get; set; } = string.Empty;

    public int RetryCount { get; set; } = 5;
}
