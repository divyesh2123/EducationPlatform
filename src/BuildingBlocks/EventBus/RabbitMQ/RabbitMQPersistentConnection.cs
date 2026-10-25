using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMQ;

/// <summary>
/// Keeps one long-lived TCP connection to RabbitMQ per service and retries with
/// exponential back-off when the broker is not reachable yet.
/// </summary>
public sealed class RabbitMQPersistentConnection : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQPersistentConnection> _logger;
    private readonly object _syncRoot = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMQPersistentConnection(IOptions<RabbitMQOptions> options, ILogger<RabbitMQPersistentConnection> logger)
    {
        _options = options.Value;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            TryConnect();
        }

        return _connection!.CreateModel();
    }

    public void TryConnect()
    {
        lock (_syncRoot)
        {
            if (IsConnected)
            {
                return;
            }

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port} (attempt {Attempt})", _options.HostName, _options.Port, attempt);
                    _connection = _factory.CreateConnection();
                    _logger.LogInformation("Connected to RabbitMQ");
                    return;
                }
                catch (BrokerUnreachableException ex) when (attempt < _options.RetryCount)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogWarning(ex, "RabbitMQ unreachable, retrying in {Delay}s", delay.TotalSeconds);
                    Thread.Sleep(delay);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _connection?.Dispose();
    }
}
