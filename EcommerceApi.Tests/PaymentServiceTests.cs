using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class PaymentServiceTests
    {
        private EcommerceContext? _context;
        private PaymentService? _service;
        private Mock<IBackgroundTaskQueue>? _taskQueueMock;

        // Méthode exécutée avant chaque test : initialise une base de données InMemory + service de paiements.
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EcommerceContext>()    // Configuration InMemory
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            var logger = new Mock<ILogger<PaymentService>>();                // Mock Logger
            _taskQueueMock = new Mock<IBackgroundTaskQueue>();

            _service = new PaymentService(_context!, _taskQueueMock.Object, logger.Object);
        }



        // Test 1 : Création d’un paiement valide
        [TestMethod]
        public async Task CreatePaymentAsync_ShouldCreatePayment()
        {
            // Arrange (créer un utilisateur, commande et items)
            var user = new User { Id = 1, Username = "user1" };
            _context!.Users.Add(user);

            var order = new Order { Id = 1, UserId = user.Id, IsPaid = false };
            _context.Orders.Add(order);

            var item = new OrderItem { Id = 1, OrderId = order.Id, Price = 10, Quantity = 2 };
            _context.OrderItems.Add(item);

            await _context.SaveChangesAsync();

            var dto = new CreatePaymentDto { OrderId = order.Id, Amount = 20 };

            // Act
            var payment = await _service!.CreatePaymentAsync(user.Id, dto);

            // Assert
            Assert.IsNotNull(payment);
            Assert.AreEqual(PaymentStatus.Pending, payment.Status);
            Assert.AreEqual(20, payment.Amount);
            _taskQueueMock!.Verify(q => q.QueueBackgroundWorkItem(payment.Id), Times.Once);
        }

        // Test 2 : GetPaymentAsync retourne le paiement correct
        [TestMethod]
        public async Task GetPaymentAsync_ShouldReturnPayment()
        {
            var payment = new Payment { Id = 1, Amount = 15, Status = PaymentStatus.Pending };
            _context!.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var result = await _service!.GetPaymentAsync(payment.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(payment.Id, result!.Id);
        }

        // Test 3 : Ne peut pas payer une commande qui n’existe pas
        [TestMethod]
        public async Task CreatePaymentAsync_NonExistingOrder_ShouldThrow()
        {
            var dto = new CreatePaymentDto { OrderId = 999, Amount = 50 };
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() =>
                _service!.CreatePaymentAsync(1, dto));
        }

        // Test 4 : Ne peut pas payer une commande qui appartient à un autre utilisateur
        [TestMethod]
        public async Task CreatePaymentAsync_OrderNotOwned_ShouldThrow()
        {
            var user = new User { Id = 1 };
            _context!.Users.Add(user);
            var order = new Order { Id = 1, UserId = 2, IsPaid = false };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var dto = new CreatePaymentDto { OrderId = order.Id, Amount = 10 };
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
                _service!.CreatePaymentAsync(user.Id, dto));
        }

        // Test 5 : Ne peut pas payer avec un montant incorrect
        [TestMethod]
        public async Task CreatePaymentAsync_WrongAmount_ShouldThrow()
        {
            var user = new User { Id = 1 };
            _context!.Users.Add(user);
            var order = new Order { Id = 1, UserId = user.Id, IsPaid = false };
            _context.Orders.Add(order);
            _context.OrderItems.Add(new OrderItem { Id = 1, OrderId = order.Id, Price = 10, Quantity = 2 });
            await _context.SaveChangesAsync();

            var dto = new CreatePaymentDto { OrderId = order.Id, Amount = 50 };            // incorrect
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                _service!.CreatePaymentAsync(user.Id, dto));
        }

        // Test 6 : Idempotence : même clé renvoie le même paiement
        [TestMethod]
        public async Task CreatePaymentAsync_Idempotency_ShouldReturnExisting()
        {
            var user = new User { Id = 1 };
            _context!.Users.Add(user);
            var order = new Order { Id = 1, UserId = user.Id, IsPaid = false };
            _context.Orders.Add(order);
            _context.OrderItems.Add(new OrderItem { Id = 1, OrderId = order.Id, Price = 10, Quantity = 2 });
            await _context.SaveChangesAsync();

            var dto1 = new CreatePaymentDto { OrderId = order.Id, Amount = 20, IdempotencyKey = "KEY1" };
            var payment1 = await _service!.CreatePaymentAsync(user.Id, dto1);

            var dto2 = new CreatePaymentDto { OrderId = order.Id, Amount = 20, IdempotencyKey = "KEY1" };
            var payment2 = await _service.CreatePaymentAsync(user.Id, dto2);

            Assert.AreEqual(payment1.Id, payment2.Id);                      // même paiement retourné
        }
    }
}
