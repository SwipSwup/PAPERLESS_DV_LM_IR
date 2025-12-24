namespace Core.Configuration
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string User { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string QueueName { get; set; } = "documents";
    }
}
