using Core.Messaging;

namespace OcrWorker.Messaging
{
    public interface IDocumentMessageProducerFactory
    {
        IDocumentMessageProducer GetIndexingProducer();
        IDocumentMessageProducer GetGenaiProducer();
    }
}

