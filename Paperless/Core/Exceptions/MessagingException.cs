namespace Core.Exceptions
{
    public class MessagingException(string message, Exception? inner = null) : Exception(message, inner);
}