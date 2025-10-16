using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Repositories;
using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class OrderServiceTests
    {
        private EcommerceContext? _context;
        private OrderService? _service;
        private Mock<ILogger<OrderService>>? _logger;


        // Méthode exécutée avant chaque test : initialise une base de données InMemory + service de commandes.
        [TestInitialize]
        public void Setup()
        {
            // Initialisation InMemory DB
            var options = new DbContextOptionsBuilder<EcommerceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            // Nettoyage de la DB avant chaque test
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _logger = new Mock<ILogger<OrderService>>();

            var orderRepo = new OrderRepository(_context);
            var productRepo = new ProductRepository(_context);

            // Service à tester
            _service = new OrderService(orderRepo, productRepo, _logger.Object);
        }


        // Test 1 : Créer une commande avec un produit en stock
        [TestMethod]
        public async Task CreateOrderAsync_ShouldCreateOrder_WhenStockIsSufficient()
        {
            // Arrange
            var product = new Product { Name = "P1", Price = 10, Stock = 5 };
            _context!.Products.Add(product);
            _context.Users.Add(new User { Id = 1, Username = "John" });
            await _context.SaveChangesAsync();

            var dto = new CreateOrderDto
            {
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = product.Id, Quantity = 2 }
                }
            };

            // Act
            var order = await _service!.CreateOrderAsync(1, dto);

            // Assert
            Assert.IsNotNull(order);
            Assert.AreEqual(1, order.OrderItems.Count);
            Assert.AreEqual(3, product.Stock);  // 5 - 2 = 3
        }

        // Test 2 : Récupérer les commandes d’un utilisateur
        [TestMethod]
        public async Task GetOrdersByUserAsync_ShouldReturnOrdersForUser()
        {
            // Arrange
            var user = new User { Id = 2, Username = "Alice" };
            _context!.Users.Add(user);
            var product = new Product { Name = "P2", Price = 20, Stock = 5 };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                UserId = user.Id,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1, Price = 20 }
                }
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var orders = await _service!.GetOrdersByUserAsync(user.Id);

            // Assert
            Assert.AreEqual(1, orders.Count);
            Assert.AreEqual(user.Id, orders.First().UserId);
        }

        // Test 3 : Récupérer une commande par ID
        [TestMethod]
        public async Task GetOrderByIdAsync_ShouldReturnOrder()
        {
            // Arrange
            var user = new User { Id = 3, Username = "Bob" };
            _context!.Users.Add(user);
            var product = new Product { Name = "P3", Price = 15, Stock = 5 };
            _context.Products.Add(product);

            var order = new Order
            {
                UserId = user.Id,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 1, Price = 15 }
                }
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            var foundOrder = await _service!.GetOrderByIdAsync(order.Id, user.Id);

            // Assert
            Assert.IsNotNull(foundOrder);
            Assert.AreEqual(order.Id, foundOrder.Id);
            Assert.AreEqual(1, foundOrder.OrderItems.Count);
        }

        // Test 4 : Mettre à jour le statut d’une commande
        [TestMethod]
        public async Task UpdateOrderStatusAsync_ShouldChangeOrderStatus()
        {
            // Arrange
            var order = new Order { UserId = 4, Status = OrderStatus.Pending };
            _context!.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Act
            await _service!.UpdateOrderStatusAsync(order.Id, OrderStatus.Shipped);

            // Assert
            var updated = await _context.Orders.FindAsync(order.Id);
            Assert.AreEqual(OrderStatus.Shipped, updated!.Status);
        }

        // Test 5 : Annuler une commande en attente
        [TestMethod]
        public async Task CancelOrderAsync_ShouldCancelAndRestoreStock()
        {
            // Arrange
            var product = new Product { Name = "P5", Price = 50, Stock = 10 };
            _context!.Products.Add(product);
            var user = new User { Id = 5, Username = "Eve" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var order = new Order
            {
                UserId = user.Id,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { ProductId = product.Id, Quantity = 3, Price = 50, Product = product }
                }
            };
            _context.Orders.Add(order);
            product.Stock -= 3;
            await _context.SaveChangesAsync();

            // Act
            await _service!.CancelOrderAsync(order.Id, user.Id);

            // Assert
            var cancelledOrder = await _context.Orders.FindAsync(order.Id);
            Assert.AreEqual(OrderStatus.Cancelled, cancelledOrder!.Status);
            Assert.AreEqual(10, product.Stock);                               // Stock restauré
        }
    }
}
