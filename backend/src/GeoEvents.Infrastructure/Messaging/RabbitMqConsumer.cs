using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeoEvents.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GeoEvents.Infrastructure.Messaging;

/// <summary>
/// Background service that consumes messages from a RabbitMQ queue and dispatches to handlers.
/// </summary>
public sealed class RabbitMqConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IEventDispatcher _dispatcher;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILogger<RabbitMqConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _queueName;

    public RabbitMqConsumer(
        IOptions<RabbitMqOptions> options,
        IEventDispatcher dispatcher,
        IIdempotencyStore idempotencyStore,
        ILogger<RabbitMqConsumer> logger,
        string queueName = "geo.unit.positions")
    {
        _options = options.Value;
        _dispatcher = dispatcher;
        _idempotencyStore = idempotencyStore;
        _logger = logger;
        _queueName = queueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);

        var consumer = new EventingBasicConsumer(_channel!);
        consumer.Received += (sender, ea) =>
        {
            Task.Run(async () =>
            {
                try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                // Deserialize to base DTO to extract eventId
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(json);
                if (envelope?.EventId == null)
                {
                    _logger.LogWarning("Received message without eventId, skipping");
                    _channel!.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // Check idempotency
                var isNew = await _idempotencyStore.TryMarkProcessedAsync(envelope.EventId, stoppingToken);
                if (!isNew)
                {
                    _logger.LogInformation("Event {EventId} already processed, skipping", envelope.EventId);
                    _channel!.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // Dispatch to handlers
                var dtoType = ResolveDtoTypeFromRoutingKey(ea.RoutingKey);
                if (dtoType != null)
                {
                    var dto = JsonSerializer.Deserialize(json, dtoType);
                    if (dto != null)
                    {
                        await _dispatcher.DispatchAsync(dto, stoppingToken);
                        _logger.LogInformation("Successfully processed event {EventId} with routing key {RoutingKey}",
                            envelope.EventId, ea.RoutingKey);
                    }
                }

                _channel!.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {Queue}", _queueName);
                // Nack without requeue on fatal error to prevent infinite loop
                _channel!.BasicNack(ea.DeliveryTag, false, false);
            }
            }, stoppingToken);
        };

        _channel!.BasicConsume(_queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("RabbitMQ consumer started for queue {Queue}", _queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare queue (should already exist from docker-compose definitions, but safe to redeclare)
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        await Task.CompletedTask;
    }

    private static Type? ResolveDtoTypeFromRoutingKey(string routingKey)
    {
        // Simple routing key to DTO type mapping
        return routingKey switch
        {
            "geo.unit.position" => typeof(Application.DTOs.UnitPositionEventDto),
            "geo.zone.violation" => typeof(Application.DTOs.ZoneViolationEventDto),
            "geo.proximity.alert" => typeof(Application.DTOs.ProximityAlertEventDto),
            _ => null
        };
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }

    private sealed record EventEnvelope(Guid EventId);
}
