using System.Text;
using System.Text.Json;
using EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventBus.RabbitMQ;

/// <summary>
/// Publishes integration events to a durable direct exchange and (as a hosted service)
/// consumes the events this service subscribed to from its own durable queue.
///
/// Topology per consuming service:
///   exchange "education_event_bus" --(routing key = event name)--> queue "{QueueName}"
///   queue "{QueueName}" --(on failure / nack)--> exchange "{QueueName}.dlx" --> queue "{QueueName}.dlq"
/// </summary>
public sealed class RabbitMQEventBus : BackgroundService, IEventBus
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMQPersistentConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EventBusSubscriptionInfo _subscriptionInfo;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private IModel? _consumerChannel;

    public RabbitMQEventBus(
        RabbitMQPersistentConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<EventBusSubscriptionInfo> subscriptionInfo,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQEventBus> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _subscriptionInfo = subscriptionInfo.Value;
        _options = options.Value;
        _logger = logger;
    }

    public void Publish(IntegrationEvent @event)
    {
        var eventName = @event.GetType().Name;

        // IModel is not thread-safe, so a short-lived channel per publish keeps things simple.
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: true);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), JsonOptions);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // persistent
        properties.ContentType = "application/json";
        properties.MessageId = @event.Id.ToString();
        properties.Type = eventName;

        channel.BasicPublish(_options.ExchangeName, eventName, mandatory: false, properties, body);

        _logger.LogInformation("Published {EventName} ({EventId})", eventName, @event.Id);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_subscriptionInfo.EventTypes.Count == 0)
        {
            _logger.LogInformation("No event subscriptions registered, consumer not started");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_options.QueueName))
        {
            throw new InvalidOperationException("RabbitMQ:QueueName must be configured for a service that has subscriptions.");
        }

        // Run on a background thread so a slow broker does not block application start-up.
        return Task.Run(() => StartConsumer(stoppingToken), stoppingToken);
    }

    private async Task StartConsumer(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _consumerChannel = _connection.CreateModel();
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not open consumer channel, retrying in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        if (_consumerChannel is null)
        {
            return;
        }

        var channel = _consumerChannel;
        var deadLetterExchange = $"{_options.QueueName}.dlx";
        var deadLetterQueue = $"{_options.QueueName}.dlq";

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: true);

        channel.ExchangeDeclare(deadLetterExchange, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(deadLetterQueue, deadLetterExchange, routingKey: string.Empty);

        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object> { ["x-dead-letter-exchange"] = deadLetterExchange });

        foreach (var eventName in _subscriptionInfo.EventTypes.Keys)
        {
            channel.QueueBind(_options.QueueName, _options.ExchangeName, routingKey: eventName);
            _logger.LogInformation("Queue {Queue} subscribed to {EventName}", _options.QueueName, eventName);
        }

        channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessageReceived;
        channel.BasicConsume(_options.QueueName, autoAck: false, consumer);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs args)
    {
        var eventName = args.RoutingKey;
        var message = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
            _consumerChannel!.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // Not re-queued: the message goes to the dead-letter queue for inspection instead of looping forever.
            _logger.LogError(ex, "Error processing {EventName}: {Message}", eventName, message);
            _consumerChannel!.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (!_subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            _logger.LogWarning("No subscription for {EventName}", eventName);
            return;
        }

        var integrationEvent = (IntegrationEvent)JsonSerializer.Deserialize(message, eventType, JsonOptions)!;
        _logger.LogInformation("Received {EventName} ({EventId})", eventName, integrationEvent.Id);

        await using var scope = _scopeFactory.CreateAsyncScope();
        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
        {
            await handler.Handle(integrationEvent);
        }
    }

    public override void Dispose()
    {
        _consumerChannel?.Dispose();
        base.Dispose();
    }
}
