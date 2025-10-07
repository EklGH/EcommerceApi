namespace EcommerceApi.Services
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(int paymentId);
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken);
    }
}
