namespace Core.Exceptions
{
    public class DataAccessException(string message, Exception? inner = null) : Exception(message, inner);
}