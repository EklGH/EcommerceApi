using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Services
{
    public class OrderService
    {
        private readonly EcommerceContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(EcommerceContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }


        // ======== CRUD logique métier

        // Crée ...une commande
        public async Task<Order> CreateOrderAsync(int userId, CreateOrderDto dto)
        {
            _logger.LogInformation("Création d'une commande pour l'utilisateur {UserId}", userId);

            var orderItems = new List<OrderItem>();

            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)                           // existence du produit
                {
                    _logger.LogWarning("Produit {ProductId} introuvable pour l'utilisateur {UserId}", item.ProductId, userId);
                    throw new Exception($"Produit {item.ProductId} introuvable");
                }
                if (product.Stock < item.Quantity)             // stock disponible
                {
                    _logger.LogWarning("Stock insuffisant pour {ProductName} (User {UserId})", product.Name, userId);
                    throw new Exception($"Stock insuffisant pour {product.Name}");
                }

                product.Stock -= item.Quantity;                // réduction stock

                orderItems.Add(new OrderItem                   // création commande avec items
                {
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    Price = product.Price
                });

                _logger.LogInformation("Produit {ProductName} ajouté à la commande (Qty: {Quantity}) pour l'utilisateur {UserId}",
                    product.Name, item.Quantity, userId);
            }

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Commande {OrderId} créée avec succès pour l'utilisateur {UserId}", order.Id, userId);

            return order;
        }

        // Recherche ...toutes les commandes d'un utilisateur
        public async Task<List<Order>> GetOrdersByUserAsync(int userId)
        {
            _logger.LogInformation("Récupération des commandes pour l'utilisateur {UserId}", userId);

            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .ToListAsync();
        }

        // Recherche ...les détails d'une commande par ID
        public async Task<Order> GetOrderByIdAsync(int orderId, int userId)
        {
            _logger.LogInformation("Récupération de la commande {OrderId} pour l'utilisateur {UserId}", orderId, userId);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                _logger.LogWarning("Commande {OrderId} introuvable pour l'utilisateur {UserId}", orderId, userId);
                throw new Exception("Commande introuvable");
            }

            return order;
        }

        // Modifie ...le statut d'une commande (Admin)
        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            _logger.LogInformation("Mise à jour du statut de la commande {OrderId} vers {Status}", orderId, status);

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                _logger.LogWarning("Commande {OrderId} introuvable pour mise à jour du statut", orderId);
                throw new Exception("Commande introuvable");
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Statut de la commande {OrderId} mis à jour avec succès vers {Status}", orderId, status);
        }


        // Annule ...une commande (Client ou Admin)
        public async Task CancelOrderAsync(int orderId, int userId, bool isAdmin = false)
        {
            _logger.LogInformation("Tentative d'annulation de la commande {OrderId} par l'utilisateur {UserId}", orderId, userId);

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Commande {OrderId} introuvable pour annulation", orderId);
                throw new Exception("Commande introuvable");
            }

            if (!isAdmin && order.UserId != userId)          // Si pas un admin, vérifie que la commande lui appartient
            {
                _logger.LogWarning("Utilisateur {UserId} non autorisé à annuler la commande {OrderId}", userId, orderId);
                throw new Exception("Accès refusé");
            }

            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)       // Si déjà livrée ou annulée
            {
                _logger.LogWarning("Commande {OrderId} déjà livrée ou annulée, annulation impossible", orderId);
                throw new Exception("Impossible d'annuler une commande déjà livrée ou annulée");
            }

            order.Status = OrderStatus.Cancelled;            // Annule la commande

            foreach (var item in order.OrderItems)           // Remet le stock
            {
                if (item.Product != null)
                {
                    item.Product.Stock += item.Quantity;
                    _logger.LogInformation("Stock du produit {ProductId} restauré de {Quantity} unités", item.ProductId, item.Quantity);
                }
                else
                {
                    _logger.LogWarning("Le produit lié à l'OrderItem {OrderItemId} est null", item.Id);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Commande {OrderId} annulée avec succès", orderId);
        }
    }
}
