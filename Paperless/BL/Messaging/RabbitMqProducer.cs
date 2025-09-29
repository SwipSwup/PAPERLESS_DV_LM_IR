using System.Text;
using System.Text.Json;
using Core.DTOs;
using Core.Messaging;
using RabbitMQ.Client;

namespace BL.Messaging
{
    public class RabbitMqProducer : IDocumentMessageProducer, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private const string QueueName = "documents";

        public RabbitMqProducer()
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "rabbitmq", 
                UserName = "admin",
                Password = "admin",
                Port = 5672
            };

            // async connect + channel creation
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            ).GetAwaiter().GetResult();
        }

        public async Task PublishAsync(DocumentMessageDto message)
        {
            string json = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                body: body
            );
        }

        public async ValueTask DisposeAsync()
        {
            await _channel.DisposeAsync();
            await _connection.DisposeAsync();
        }

        public async Task PublishDocumentAsync(DocumentMessageDto message)
        {
            string json = JsonSerializer.Serialize(message);
            byte[] body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                body: body
            );
        }
    }
}