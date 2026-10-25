using EventBus.Abstractions;
using EventBus.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventBus.Extensions;

public interface IEventBusBuilder
{
    IServiceCollection Services { get; }
}

internal sealed class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
{
    public IServiceCollection Services { get; } = services;
}

public static class EventBusServiceCollectionExtensions
{
    /// <summary>
    /// Registers IEventBus (publisher) and the background consumer, bound to the "RabbitMQ" config section.
    /// </summary>
    public static IEventBusBuilder AddRabbitMQEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RabbitMQOptions>(configuration.GetSection(RabbitMQOptions.SectionName));
        services.AddSingleton<RabbitMQPersistentConnection>();
        services.AddSingleton<RabbitMQEventBus>();
        services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<RabbitMQEventBus>());
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMQEventBus>());

        return new EventBusBuilder(services);
    }

    /// <summary>
    /// Subscribes this service's queue to <typeparamref name="TEvent"/> and routes it to <typeparamref name="THandler"/>.
    /// </summary>
    public static IEventBusBuilder AddSubscription<TEvent, THandler>(this IEventBusBuilder builder)
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        builder.Services.AddKeyedScoped<IIntegrationEventHandler, THandler>(typeof(TEvent));
        builder.Services.Configure<EventBusSubscriptionInfo>(o => o.EventTypes[typeof(TEvent).Name] = typeof(TEvent));

        return builder;
    }
}
