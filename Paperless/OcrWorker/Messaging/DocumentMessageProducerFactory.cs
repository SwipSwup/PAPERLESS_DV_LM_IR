using Core.Configuration;
using Core.Messaging;
using BL.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OcrWorker.Messaging
{
    public class DocumentMessageProducerFactory(IOptions<RabbitMqSettings> settings, ILoggerFactory loggerFactory) : IDocumentMessageProducerFactory
    {
        private readonly RabbitMqSettings _baseSettings = settings.Value;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;
        private IDocumentMessageProducer? _indexingProducer;
        private IDocumentMessageProducer? _genaiProducer;

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
            _indexingProducer = new RabbitMqProducer(producerSettings, _loggerFactory.CreateLogger<RabbitMqProducer>());
            return _indexingProducer;
        }

        public IDocumentMessageProducer GetGenaiProducer()
        {
            if (_genaiProducer != null) 
                return _genaiProducer;
            
            RabbitMqSettings producerSettings = new RabbitMqSettings
            {
                Host = _baseSettings.Host,
                Port = _baseSettings.Port,
                User = _baseSettings.User,
                Password = _baseSettings.Password,
                QueueName = "genai"
            };
            
            _genaiProducer = new RabbitMqProducer(producerSettings, _loggerFactory.CreateLogger<RabbitMqProducer>());
            return _genaiProducer;
        }
    }
}

