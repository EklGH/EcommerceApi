using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly EcommerceContext _context;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(EcommerceContext context, IBackgroundTaskQueue taskQueue, ILogger<PaymentService> logger)
        {
            _context = context;
            _taskQueue = taskQueue;
            _logger = logger;
        }


        // ======== CRUD logique métier

        // Crée ...un paiement
        public async Task<Payment> CreatePaymentAsync(int userId, CreatePaymentDto dto)
        {
            var order = await _context.Orders                                          // Récupère la commande avec ses items
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
            {
                _logger.LogWarning("Tentative de paiement sur une commande inexistante (OrderId {OrderId}) par User {UserId}", dto.OrderId, userId);
                throw new KeyNotFoundException("Commande introuvable.");
            }
            if (order.UserId != userId)
            {
                _logger.LogWarning("Utilisateur {UserId} a tenté de payer une commande qui ne lui appartient pas (OrderId {OrderId})", userId, order.Id);
                throw new UnauthorizedAccessException("Cette commande n'appartient pas à l'utilisateur.");
            }
            if (order.IsPaid)
            {
                _logger.LogWarning("Tentative de double paiement sur la commande {OrderId}", order.Id);
                throw new InvalidOperationException("Commande déjà payée.");
            }

            var expectedAmount = order.OrderItems.Sum(i => i.Price * i.Quantity);      // Vérifie que le montant correspond
            if (dto.Amount != expectedAmount)
            {
                _logger.LogWarning("Montant incorrect pour le paiement de la commande {OrderId} par User {UserId}. Attendu {Expected}, reçu {Received}", order.Id, userId, expectedAmount, dto.Amount);
                throw new InvalidOperationException($"Montant incorrect (attendu {expectedAmount}).");
            }

            if (!string.IsNullOrEmpty(dto.IdempotencyKey))                             // Gestion de l'idempotence
            {
                var existing = await _context.Payments
                    .FirstOrDefaultAsync(p => p.IdempotencyKey == dto.IdempotencyKey);
                if (existing != null)
                {
                    _logger.LogInformation("Paiement existant réutilisé (PaymentId {PaymentId}, IdempotencyKey {Key})", existing.Id, dto.IdempotencyKey);
                    return existing;
                }
            }

            var payment = new Payment                                                  // Création du paiement
            {
                OrderId = order.Id,
                Amount = dto.Amount,
                Status = PaymentStatus.Pending,
                IdempotencyKey = dto.IdempotencyKey
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _taskQueue.QueueBackgroundWorkItem(payment.Id);                             // Mettre en queue pour traitement asynchrone

            _logger.LogInformation("Paiement créé {PaymentId} pour order {OrderId}", payment.Id, order.Id);
            return payment;
        }

        // Recherche ...un paiement par ID
        public Task<Payment?> GetPaymentAsync(int paymentId)
            => _context.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
    }
}
