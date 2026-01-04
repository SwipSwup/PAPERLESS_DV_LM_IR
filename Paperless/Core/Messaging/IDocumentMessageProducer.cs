using Core.DTOs;
using Core.DTOs.Messaging;

namespace Core.Messaging
{
    public interface IDocumentMessageProducer
    {
        Task PublishDocumentAsync(DocumentMessageDto message);
    }
}