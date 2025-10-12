using System.Text;
using System.Text.Json;
using Core.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OcrWorker;

public class Worker(ILogger<Worker> logger)
    : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;
    private const string QueueName = "documents";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await InitializeRabbitMqAsync();

            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel!);

            consumer.ReceivedAsync += async (sender, args) =>
            {
                try
                {
                    string json = Encoding.UTF8.GetString(args.Body.Span);

                    DocumentMessageDto? documentMessage = JsonSerializer.Deserialize<DocumentMessageDto>(json);

                    logger.LogInformation("Received document message: {docId}", documentMessage?.DocumentId);

                    logger.LogInformation("OCR placeholder executed for document {docId}", documentMessage?.DocumentId);

                    await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message");
                    await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            };

            await _channel!.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer, 
                cancellationToken: stoppingToken
                );

            logger.LogInformation("OCR Worker started and listening on queue '{queue}'", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Worker failed to start");
        }
    }

    private async Task InitializeRabbitMqAsync()
    {
        try
        {
            ConnectionFactory factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "admin",
                Password = "admin",
                Port = 5672
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            logger.LogInformation("Connected to RabbitMQ and declared queue '{queue}'", QueueName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize RabbitMQ connection or channel");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping OCR Worker...");
        await _channel.CloseAsync(cancellationToken: cancellationToken);
        await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.DisposeAsync().GetAwaiter().GetResult();
        _connection?.DisposeAsync().GetAwaiter().GetResult();
        base.Dispose();
    }
}