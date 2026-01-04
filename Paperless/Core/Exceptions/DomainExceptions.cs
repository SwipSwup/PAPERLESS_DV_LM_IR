namespace Core.Exceptions
{
    public abstract class DomainException(string message) : DmsException(message);

    public class EntityNotFoundException(string entityName, object key)
        : DomainException($"Entity '{entityName}' with key '{key}' was not found.");

    public class DmsValidationException(string message) : DomainException(message);
}