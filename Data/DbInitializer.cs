using EcommerceApi.Models;
using BCrypt.Net;

namespace EcommerceApi.Data
{
    public class DbInitializer      // Fichier de seeding (optionel) -> pour tests rapide
    {
        public static void Initialize(EcommerceContext context)
        {
            context.Database.EnsureCreated();
            if (context.Users.Any()) return;           // ne pas reseeder si déjà des utilisateurs en DB

            string adminPassword = "Admin123!";        // mots de passe en clair pour les tests
            string clientPassword = "Client123!";
            var users = new List<User>
        {
                new User
                {
                    Username="admin",
                    Email="admin@test.com",
                    PasswordHash=BCrypt.Net.BCrypt.HashPassword(adminPassword),
                    Role="Admin"
                },
                new User
                {
                    Username="client",
                    Email="client@test.com",
                    PasswordHash=BCrypt.Net.BCrypt.HashPassword(clientPassword),
                    Role="Client"
                }
            };

            context.Users.AddRange(users);

            var products = new List<Product>
        {
            new Product { Name="Produit 1", Description="Description 1", Price=10, Stock=100, Category="Cat 1" },
            new Product { Name="Produit 2", Description="Description 2", Price=20, Stock=50, Category="Cat 2" }
        };

            context.Products.AddRange(products);

            context.SaveChanges();
        }
    }
}