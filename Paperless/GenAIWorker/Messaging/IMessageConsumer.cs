namespace GenAIWorker.Messaging
{
    public interface IMessageConsumer
    {
        Task ConsumeAsync<T>(
            string queueName,
            Func<T, ulong, CancellationToken, Task> onMessage,
            CancellationToken ct);
    }
}

