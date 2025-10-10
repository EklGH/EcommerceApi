using EcommerceApi.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class BackgroundTaskQueueTests
    {
        private BackgroundTaskQueue? _queue;

        // Méthode exécutée avant chaque test
        [TestInitialize]
        public void Setup()
        {
            _queue = new BackgroundTaskQueue();
        }


        // Test 1 : Vérifie que lorsqu’on ajoute un paiement dans la file d’attente, un DequeueAsync récupère bien le même ID.
        [TestMethod]
        public async Task QueueAndDequeue_ShouldReturnSamePaymentId()
        {
            int paymentId = 42;

            _queue!.QueueBackgroundWorkItem(paymentId);

            int result = await _queue.DequeueAsync(CancellationToken.None);

            Assert.AreEqual(paymentId, result);
        }

        // Test 2 : Vérifie que DequeueAsync attend qu’un élément arrive si la file est vide, puis récupère correctement le paiement ajouté après.
        [TestMethod]
        public async Task DequeueAsync_ShouldWaitForItem()
        {
            var cts = new CancellationTokenSource();
            var dequeueTask = _queue!.DequeueAsync(cts.Token);

            await Task.Delay(100);        // si tjrs pas d'élément, on ajoute un délai
            _queue.QueueBackgroundWorkItem(99);

            int result = await dequeueTask;
            Assert.AreEqual(99, result);
        }
    }
}
