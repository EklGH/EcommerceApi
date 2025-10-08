using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data
{
    public class EcommerceContext : DbContext
    {
        public EcommerceContext(DbContextOptions<EcommerceContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }                              // table Users créée dans la DataBase
        public DbSet<Product> Products { get; set; }                        // "" Products
        public DbSet<Order> Orders { get; set; }                            // "" Orders
        public DbSet<OrderItem> OrderItems { get; set; }                    // "" OrderItems
        public DbSet<Payment> Payments { get; set; }                        // "" Payments

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // Décimales : précision pour SQL Server
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);          // 18 chiffres au total, 2 après la virgule

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);


            // Conversion enum->string pour Order.Status
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion(
                v => v.ToString(),
                v => ConvertToOrderStatus(v));


            // Relations
            modelBuilder.Entity<Order>()            // Order 1:1 Payment
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId);

            modelBuilder.Entity<User>()             // User 1:n Order
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<OrderItem>()        // OrderItem n:1 Product
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId);

            modelBuilder.Entity<OrderItem>()        // Order 1:n OrderItem
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);
        }



        // Méthode de conversion string->OrderStatus
        private static OrderStatus ConvertToOrderStatus(string value)
        {
            return value switch
            {
                "En attente" => OrderStatus.Pending,
                "Pending" => OrderStatus.Pending,
                "Expédiée" => OrderStatus.Shipped,
                "Shipped" => OrderStatus.Shipped,
                "Livrée" => OrderStatus.Delivered,
                "Delivered" => OrderStatus.Delivered,
                "Annulée" => OrderStatus.Cancelled,
                "Cancelled" => OrderStatus.Cancelled,
                _ => throw new ArgumentOutOfRangeException($"Statut inconnu {value}")
            };
        }
    }
}
