using Core.Configuration;
using Core.Messaging;
using BL.Messaging;
using Microsoft.Extensions.Options;

namespace OcrWorker.Messaging
{
    public class DocumentMessageProducerFactory : IDocumentMessageProducerFactory
    {
        private readonly RabbitMqSettings _baseSettings;
        private IDocumentMessageProducer? _indexingProducer;
        private IDocumentMessageProducer? _genaiProducer;

        public DocumentMessageProducerFactory(IOptions<RabbitMqSettings> settings)
        {
            _baseSettings = settings.Value;
        }

        public IDocumentMessageProducer GetIndexingProducer()
        {
            if (_indexingProducer == null)
            {
                RabbitMqSettings producerSettings = new RabbitMqSettings
                {
                    Host = _baseSettings.Host,
                    Port = _baseSettings.Port,
                    User = _baseSettings.User,
                    Password = _baseSettings.Password,
                    QueueName = "indexing"
                };
                _indexingProducer = new RabbitMqProducer(producerSettings);
            }
            return _indexingProducer;
        }

        public IDocumentMessageProducer GetGenaiProducer()
        {
            if (_genaiProducer == null)
            {
                RabbitMqSettings producerSettings = new RabbitMqSettings
                {
                    Host = _baseSettings.Host,
                    Port = _baseSettings.Port,
                    User = _baseSettings.User,
                    Password = _baseSettings.Password,
                    QueueName = "genai"
                };
                _genaiProducer = new RabbitMqProducer(producerSettings);
            }
            return _genaiProducer;
        }
    }
}

