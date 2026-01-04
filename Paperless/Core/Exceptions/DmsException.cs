using System;

namespace Core.Exceptions
{
    public abstract class DmsException : Exception
    {
        public string? CorrelationId { get; set; }

        protected DmsException(string message) : base(message)
        {
        }

        protected DmsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}