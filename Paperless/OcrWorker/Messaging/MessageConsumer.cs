using Microsoft.Extensions.Options;
using Core.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OcrWorker.Messaging
{
    public class MessageConsumer(
        ILogger<MessageConsumer> logger,
        IOptions<RabbitMqSettings> settings) : IMessageConsumer
    {
        private readonly RabbitMqSettings _settings = settings.Value;

        private IConnection? _connection;
        private IChannel? _channel;
        private IAsyncBasicConsumer? _consumer; // Keep reference

        public async Task ConsumeAsync<T>(
            string queueName,
            Func<T, ulong, CancellationToken, Task> onMessage,
            CancellationToken ct)
        {
            await EnsureConnectedAsync();

            await _channel!.BasicQosAsync(0, 1, false);

            _consumer = new OcrConsumer<T>(_channel!, logger, onMessage, ct);

            string consumerTag = await _channel!.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: _consumer,
                cancellationToken: ct);

            logger.LogInformation("RabbitMQ Consumer started on queue '{queueName}'. Consumer Tag: {Tag}", queueName, consumerTag);
        }

        private async Task EnsureConnectedAsync()
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            logger.LogInformation("Connecting to RabbitMQ at {host}:{port}...",
                _settings.Host, _settings.Port);

            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.User,
                Password = _settings.Password
            };

            _connection = await factory.CreateConnectionAsync();
            _connection.ConnectionShutdownAsync += async (o, e) =>
                logger.LogWarning("RabbitMQ Connection Shutdown: {Reason}", e.ReplyText);

            _channel = await _connection.CreateChannelAsync();
            _channel.ChannelShutdownAsync += async (o, e) =>
                logger.LogWarning("RabbitMQ Channel Shutdown: {Reason}", e.ReplyText);

            await _channel.QueueDeclareAsync(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            logger.LogInformation("Connected to RabbitMQ and declared queue '{queue}'", _settings.QueueName);
        }

        public void Dispose()
        {
            _channel?.DisposeAsync().AsTask().Wait();
            _connection?.DisposeAsync().AsTask().Wait();
        }

        // Inner class implementation
        private class OcrConsumer<T>(
            IChannel channel,
            ILogger logger,
            Func<T, ulong, CancellationToken, Task> onMessage,
            CancellationToken appToken) : IAsyncBasicConsumer
        {
            public IChannel Channel => channel;

            public async Task HandleBasicDeliverAsync(
                string consumerTag,
                ulong deliveryTag,
                bool redelivered,
                string exchange,
                string routingKey,
                IReadOnlyBasicProperties properties,
                ReadOnlyMemory<byte> body,
                CancellationToken cancellationToken)
            {
                logger.LogInformation("Message received! Tag: {Tag}", deliveryTag);

                try
                {
                    string json = Encoding.UTF8.GetString(body.Span);
                    logger.LogDebug("Message Body: {Body}", json);

                    T? message = JsonSerializer.Deserialize<T>(json);

                    if (message == null)
                    {
                        logger.LogWarning("Received null message - Nacking");
                        await Channel.BasicNackAsync(deliveryTag, false, false, appToken);
                        return;
                    }

                    await onMessage(message, deliveryTag, appToken);

                    await Channel.BasicAckAsync(deliveryTag, false, appToken);
                    logger.LogInformation("Message acknowledged. Tag: {Tag}", deliveryTag);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message");
                    await Channel.BasicNackAsync(deliveryTag, false, true, appToken);
                }
            }

            public Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task HandleBasicRecoverOkAsync(string consumerTag, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task HandleChannelShutdownAsync(object model, ShutdownEventArgs reason) => Task.CompletedTask;
        }
    }
}