using EcommerceApi.DTOs;

namespace EcommerceApi.Models
{
    public class Order                        // order = commandes
    {
        public int Id { get; set; }
        public int UserId { get; set; }                                // relation : Clé étrangère vers User.id
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;     // date commande par défaut
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public bool IsPaid { get; set; } = false;
        public User User { get; set; } = null!;                        // relation vers User
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();   // relation vers OrderItems.
        public Payment? Payment { get; set; }                          // relation vers Payment.
    }
}
