using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OcrWorker.Config;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OcrWorker.Messaging
{
    public class MessageConsumer(
        ILogger<MessageConsumer> logger,
        IOptions<RabbitMqSettings> settings) : IMessageConsumer
    {
        private readonly RabbitMqSettings _settings = settings.Value;

        private IConnection? _connection;
        private IChannel? _channel;

        public async Task ConsumeAsync<T>(
            string queueName,
            Func<T, ulong, CancellationToken, Task> onMessage,
            CancellationToken ct)
        {
            await EnsureConnectedAsync();

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (_, args) =>
            {
                try
                {
                    string json = Encoding.UTF8.GetString(args.Body.Span);

                    T? message = JsonSerializer.Deserialize<T>(json);
                    if (message == null)
                    {
                        logger.LogWarning("Received null message");
                        await _channel!.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: false,
                            cancellationToken: ct
                        );
                        return;
                    }

                    await onMessage(message, args.DeliveryTag, ct);

                    await _channel!.BasicAckAsync(args.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message");

                    // Requeue=true ensures retry if it's a temporary issue
                    await _channel!.BasicNackAsync(args.DeliveryTag, false, requeue: true, ct);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: queueName,
                autoAck: false, // Manual ack ensures reliability
                consumer: consumer,
                cancellationToken: ct);

            logger.LogInformation("RabbitMQ Consumer started on queue '{queueName}'", queueName);
        }

        /// <summary>
        /// Ensures RabbitMQ connection + channel are created.
        /// Automatically reconnects if connection was lost.
        /// </summary>
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
                Password = _settings.Password,
                //DispatchConsumersAsync = true
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

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
    }
}