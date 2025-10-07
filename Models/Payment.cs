using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EcommerceApi.Models
{
    public enum PaymentStatus
    {
        Pending,                // en attente (créé mais pas confirmé)
        Confirmed,
        Failed
    }
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // date de création
        public DateTime? ProcessedAt { get; set; }                  // date de traitement
        public string? IdempotencyKey { get; set; }                 // évite le double envoi
    }
}
