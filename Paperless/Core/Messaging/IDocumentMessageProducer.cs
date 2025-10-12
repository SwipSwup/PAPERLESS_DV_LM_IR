using Core.DTOs;

namespace Core.Messaging
{
    public interface IDocumentMessageProducer
    {
        Task PublishDocumentAsync(DocumentMessageDto message);
    }
}