using System.Text;
using System.Text.Json;
using Notifications.Api.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts;

namespace Notifications.Api.Messaging;

public class OrderCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<OrderCreatedConsumer> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = config["RabbitMQ:Host"] ?? "rabbitmq" };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch (Exception ex) when (attempt < 10)
            {
                logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}), retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        await _channel!.ExchangeDeclareAsync("orders", ExchangeType.Fanout, durable: true, cancellationToken: stoppingToken);
        var queue = await _channel.QueueDeclareAsync("notifications.order-created", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(queue.QueueName, "orders", string.Empty, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            if (evt is not null)
            {
                using var scope = scopeFactory.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notifications.CreateFromOrderAsync(evt, stoppingToken);
                logger.LogInformation("Notification recorded for order {OrderId}", evt.OrderId);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(queue.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
