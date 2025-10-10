using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Net;

namespace EcommerceApi.Services
{
    public class PaymentProcessingBackgroundService : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        protected readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentProcessingBackgroundService> _logger;
        private readonly Random _rng = new Random();

        public PaymentProcessingBackgroundService(
            IBackgroundTaskQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentProcessingBackgroundService> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // Boucle de traitement des paiements en file d'attente (et met a jour leur statut)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Payment processor started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                int paymentId;

                try
                {
                    paymentId = await _queue.DequeueAsync(stoppingToken);       // Récupère le prochain paiement
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                using var scope = _scopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

                var payment = await ctx.Payments.Include(p => p.Order)
                                               .FirstOrDefaultAsync(p => p.Id == paymentId, stoppingToken);
                if (payment == null) continue;

                await Task.Delay(_rng.Next(500, 3000), stoppingToken);          // Simule un délai de traitement (entre 0.5 et 3sec)

                var success = _rng.NextDouble() > 0.2;                          // Détermine succès/échec du paiement (80% de réussite)

                using var tx = await ctx.Database.BeginTransactionAsync(stoppingToken);    // Transaction : met à jour Payment et Order
                try
                {
                    payment.Status = success ? PaymentStatus.Confirmed : PaymentStatus.Failed;
                    payment.ProcessedAt = DateTime.UtcNow;
                    ctx.Payments.Update(payment);

                    if (success)
                    {
                        payment.Order.IsPaid = true;
                        ctx.Orders.Update(payment.Order);
                    }

                    await ctx.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);

                    _logger.LogInformation("Payment {PaymentId} processed: {Status}", payment.Id, payment.Status);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(stoppingToken);
                    _logger.LogError(ex, "Erreur lors du traitement du paiement {PaymentId}", paymentId);
                }
            }
        }

        // Extrait le traitement d’un paiement pour tests unitaires.
        protected virtual async Task ProcessNextPaymentAsync(CancellationToken token)
        {
            int paymentId = await _queue.DequeueAsync(token);

            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<EcommerceContext>();

            var payment = await ctx.Payments.Include(p => p.Order)
                                            .FirstOrDefaultAsync(p => p.Id == paymentId, token);
            if (payment == null) return;
        }
    }
}
