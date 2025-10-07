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

            modelBuilder.Entity<Order>()            // Conversion enum->string pour Order.Status
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Order>()            // Relation 1:1 entre Order et Payment
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderId);

            modelBuilder.Entity<User>()             // Relation 1:n entre User et Order
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<OrderItem>()        // Relation n:1 entre OrderItem et Product
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId);

            modelBuilder.Entity<OrderItem>()        // Relation 1:n entre Order et OrderItem
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);
        }
    }
}
