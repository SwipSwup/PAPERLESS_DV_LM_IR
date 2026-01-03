using System.Text;
using Core.Configuration;
using System.Text.Json;
using Core.DTOs;
using Core.Exceptions;
using Core.Messaging;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace BL.Messaging
{
    public class RabbitMqProducer : IDocumentMessageProducer, IAsyncDisposable
    {
        private readonly ILogger<RabbitMqProducer> _logger;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;

        public RabbitMqProducer(RabbitMqSettings settings, ILogger<RabbitMqProducer> logger)
        {
            _logger = logger;
            try
            {
                _logger.LogInformation("RabbitMqProducer: Initializing connection to {Host}:{Port}", settings.Host, settings.Port);
                _queueName = settings.QueueName;

                ConnectionFactory factory = new ConnectionFactory
                {
                    HostName = settings.Host,
                    UserName = settings.User,
                    Password = settings.Password,
                    Port = settings.Port
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false
                ).GetAwaiter().GetResult();



                _logger.LogInformation("RabbitMqProducer: Connection and channel initialized");
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
                _logger.LogInformation("RabbitMqProducer: Publishing message for Document ID {Id}", message.DocumentId);
                string json = JsonSerializer.Serialize(message);
                byte[] body = Encoding.UTF8.GetBytes(json);

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _queueName,
                    body: body
                );



                _logger.LogInformation("RabbitMqProducer: Message published for Document ID {Id}", message.DocumentId);
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
                _logger.LogInformation("RabbitMqProducer: Disposing connection and channel");
                await _channel.DisposeAsync();
                await _connection.DisposeAsync();
                _logger.LogInformation("RabbitMqProducer: Disposed successfully");
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
                _logger.LogInformation("RabbitMqProducer: Publishing document message for Document ID {Id}", message.DocumentId);
                string json = JsonSerializer.Serialize(message);
                byte[] body = Encoding.UTF8.GetBytes(json);

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: _queueName,
                    body: body
                );



                _logger.LogInformation("RabbitMqProducer: Document message published for Document ID {Id}", message.DocumentId);
            }
            catch (Exception ex)
            {
                throw new MessagingException($"Failed to publish document message for Document ID {message.DocumentId}.", ex);
            }
        }
    }
}
