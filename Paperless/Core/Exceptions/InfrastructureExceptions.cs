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

        public InfrastructureException(string message, Exception inner, bool isTransient = false) 
            : base(message, inner) 
        {
            IsTransient = isTransient;
        }
    }

    public class StorageException : InfrastructureException
    {
        public StorageException(string message, Exception inner) 
            : base(message, inner, isTransient: false) { }
    }
    
    public class OcrGenerationException : InfrastructureException
    {
        public OcrGenerationException(string message, Exception inner, bool isTransient) 
            : base(message, inner, isTransient) { }
    }
}
