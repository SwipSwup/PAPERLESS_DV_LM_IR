namespace OcrWorker.Messaging
{
    public interface IMessageConsumer
    {
        public Task ConsumeAsync<T>(
            string queueName,
            Func<T, ulong, CancellationToken, Task> onMessage,
            CancellationToken ct);
    }
}