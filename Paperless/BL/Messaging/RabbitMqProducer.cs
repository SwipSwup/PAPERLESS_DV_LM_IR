using System.Text;
using System.Text.Json;
using Core.DTOs;
using Core.Exceptions;
using Core.Messaging;
using RabbitMQ.Client;
using log4net;
using System.Reflection;

namespace BL.Messaging
{
    public class RabbitMqProducer : IDocumentMessageProducer, IAsyncDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType);

        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "documents";

        public RabbitMqProducer()
        {
            try
            {
                log.Info("RabbitMqProducer: Initializing connection");

                ConnectionFactory factory = new ConnectionFactory
                {
                    HostName = "rabbitmq",
                    UserName = "admin",
                    Password = "admin",
                    Port = 5672
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _channel.QueueDeclareAsync(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                ).GetAwaiter().GetResult();

                log.Info("RabbitMqProducer: Connection and channel initialized");
            }
            catch (Exception ex)
            {
                throw new MessagingException("Failed to initialize RabbitMQ connection or channel.", ex);
            }
        }

        public async Task PublishAsync(DocumentMessageDto message)
        {
            try
            {
                log.Info($"RabbitMqProducer: Publishing message for Document ID {message.DocumentId}");
                string json = JsonSerializer.Serialize(message);
                byte[] body = Encoding.UTF8.GetBytes(json);

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QueueName,
                    body: body
                );

                log.Info($"RabbitMqProducer: Message published for Document ID {message.DocumentId}");
            }
            catch (Exception ex)
            {
                throw new MessagingException($"Failed to publish message for Document ID {message.DocumentId}.", ex);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                log.Info("RabbitMqProducer: Disposing connection and channel");
                await _channel.DisposeAsync();
                await _connection.DisposeAsync();
                log.Info("RabbitMqProducer: Disposed successfully");
            }
            catch (Exception ex)
            {
                throw new MessagingException("Failed to dispose RabbitMQ connection or channel.", ex);
            }
        }

        public async Task PublishDocumentAsync(DocumentMessageDto message)
        {
            try
            {
                log.Info($"RabbitMqProducer: Publishing document message for Document ID {message.DocumentId}");
                string json = JsonSerializer.Serialize(message);
                byte[] body = Encoding.UTF8.GetBytes(json);

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: QueueName,
                    body: body
                );

                log.Info($"RabbitMqProducer: Document message published for Document ID {message.DocumentId}");
            }
            catch (Exception ex)
            {
                throw new MessagingException($"Failed to publish document message for Document ID {message.DocumentId}.", ex);
            }
        }
    }
}
