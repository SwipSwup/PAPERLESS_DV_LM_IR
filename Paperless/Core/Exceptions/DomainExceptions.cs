namespace Core.Exceptions
{
    public abstract class DomainException : DmsException
    {
        protected DomainException(string message) : base(message) { }
    }

    public class EntityNotFoundException : DomainException
    {
        public EntityNotFoundException(string entityName, object key) 
            : base($"Entity '{entityName}' with key '{key}' was not found.") { }
    }
    
    public class DmsValidationException : DomainException
    {
        public DmsValidationException(string message) : base(message) { }
    }
}
