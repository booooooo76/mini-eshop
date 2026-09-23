using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Shared.Contracts;

namespace Orders.Api.Messaging;

public class RabbitMqOrderPublisher(IConfiguration config, ILogger<RabbitMqOrderPublisher> logger)
    : IOrderPublisher, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is not null)
        {
            return _channel;
        }

        await _initLock.WaitAsync(ct);
        try
        {
            if (_channel is not null)
            {
                return _channel;
            }

            var factory = new ConnectionFactory { HostName = config["RabbitMQ:Host"] ?? "rabbitmq" };

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    _connection = await factory.CreateConnectionAsync(ct);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                    await _channel.ExchangeDeclareAsync("orders", ExchangeType.Fanout, durable: true, cancellationToken: ct);
                    return _channel;
                }
                catch (Exception ex) when (attempt < 10)
                {
                    logger.LogWarning(ex, "RabbitMQ not ready (attempt {Attempt}), retrying...", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync(OrderCreatedEvent evt, CancellationToken ct = default)
    {
        var channel = await GetChannelAsync(ct);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await channel.BasicPublishAsync("orders", string.Empty, body, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }

        GC.SuppressFinalize(this);
    }
}
