using System;

namespace Core.Exceptions
{
    public class InfrastructureException : DmsException
    {
        public bool IsTransient { get; }

        public InfrastructureException(string message, bool isTransient = false) 
            : base(message) 
        {
            IsTransient = isTransient;
        }

        protected InfrastructureException(string message, Exception inner, bool isTransient = false) 
            : base(message, inner) 
        {
            IsTransient = isTransient;
        }
    }

    public class StorageException(string message, Exception inner)
        : InfrastructureException(message, inner, isTransient: false);
    
    public class OcrGenerationException(string message, Exception inner, bool isTransient)
        : InfrastructureException(message, inner, isTransient);
}
