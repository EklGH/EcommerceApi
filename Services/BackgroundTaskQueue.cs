using System.Threading.Channels;

namespace EcommerceApi.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue            // File d'attente des paiements
    {
        private readonly Channel<int> _queue = Channel.CreateUnbounded<int>();


        // Ajoute un paiement à la queue
        public void QueueBackgroundWorkItem(int paymentId)
        {
            _queue.Writer.TryWrite(paymentId);
        }

        // Récupère le prochain paiement de la queue
        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
