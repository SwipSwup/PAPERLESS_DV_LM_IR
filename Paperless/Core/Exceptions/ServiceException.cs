namespace Core.Exceptions
{
    public class ServiceException(string message, Exception? inner = null) : Exception(message, inner);
}