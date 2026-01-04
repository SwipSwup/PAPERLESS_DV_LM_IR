using Core.Configuration;
using Core.Messaging;
using BL.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OcrWorker.Messaging
{
    public class DocumentMessageProducerFactory(IOptions<RabbitMqSettings> settings, ILoggerFactory loggerFactory)
        : IDocumentMessageProducerFactory
    {
        private readonly RabbitMqSettings _baseSettings = settings.Value;
        private IDocumentMessageProducer? _indexingProducer;
        private IDocumentMessageProducer? _genAiProducer;

        public IDocumentMessageProducer GetIndexingProducer()
        {
            if (_indexingProducer != null)
                return _indexingProducer;

            RabbitMqSettings producerSettings = new RabbitMqSettings
            {
                Host = _baseSettings.Host,
                Port = _baseSettings.Port,
                User = _baseSettings.User,
                Password = _baseSettings.Password,
                QueueName = "indexing"
            };
            _indexingProducer = new RabbitMqProducer(producerSettings, loggerFactory.CreateLogger<RabbitMqProducer>());
            return _indexingProducer;
        }

        public IDocumentMessageProducer GetGenaiProducer()
        {
            if (_genAiProducer != null)
                return _genAiProducer;

            RabbitMqSettings producerSettings = new RabbitMqSettings
            {
                Host = _baseSettings.Host,
                Port = _baseSettings.Port,
                User = _baseSettings.User,
                Password = _baseSettings.Password,
                QueueName = "genai"
            };

            _genAiProducer = new RabbitMqProducer(producerSettings, loggerFactory.CreateLogger<RabbitMqProducer>());
            return _genAiProducer;
        }
    }
}