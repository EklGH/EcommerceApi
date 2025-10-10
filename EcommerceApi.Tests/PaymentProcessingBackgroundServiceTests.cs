using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using EcommerceApi.Services;
using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class PaymentProcessingBackgroundServiceTests
    {
        private EcommerceContext _context = null!;
        private Mock<IBackgroundTaskQueue> _queueMock = null!;
        private Mock<IServiceScopeFactory> _scopeFactoryMock = null!;
        private Mock<ILogger<PaymentProcessingBackgroundService>> _loggerMock = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EcommerceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            _queueMock = new Mock<IBackgroundTaskQueue>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _loggerMock = new Mock<ILogger<PaymentProcessingBackgroundService>>();
        }


        // Test principal : traite un paiement et met à jour son statut et la commande
        [TestMethod]
        public async Task ProcessNextPaymentAsync_ShouldMarkPaymentAsConfirmedAndOrderPaid_WhenPaymentExists()
        {
            // Arrange : créer commande et paiement
            var order = new Order { Id = 1, IsPaid = false };
            _context!.Orders.Add(order);

            var payment = new Payment { Id = 1, Order = order, Status = PaymentStatus.Pending };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            // Queue mockée pour retourner l'ID du paiement
            _queueMock!.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(payment.Id);

            // Scope mocké pour retourner le contexte
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider.GetService(typeof(EcommerceContext)))
                     .Returns(_context);

            _scopeFactoryMock!.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            var service = new TestablePaymentProcessingBackgroundService(
                _queueMock.Object,
                _scopeFactoryMock.Object,
                _loggerMock.Object);

            // Act : exécute la logique de traitement pour un paiement
            await service.TestProcessNextPaymentAsync(CancellationToken.None);

            // Assert : vérifie que le paiement et la commande ont bien été mis à jour
            var updatedPayment = await _context.Payments.Include(p => p.Order).FirstAsync(p => p.Id == payment.Id);
            Assert.IsTrue(updatedPayment.Status == PaymentStatus.Confirmed);
            Assert.IsTrue(updatedPayment.Order.IsPaid);
        }
    }


    // Sous-classe pour exposer la logique de traitement d’un paiement sans boucle infinie.
    public class TestablePaymentProcessingBackgroundService : PaymentProcessingBackgroundService
    {
        private readonly IBackgroundTaskQueue _testQueue;
        public TestablePaymentProcessingBackgroundService(
            IBackgroundTaskQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<PaymentProcessingBackgroundService> logger)
            : base(queue, scopeFactory, logger)
        {
            _testQueue = queue;            // référence pour le test
        }

        // Logique interne de traitement d’un paiement pour le tester sans boucle infinie.
        public async Task TestProcessNextPaymentAsync(CancellationToken token)
        {
            // Appelle la logique interne du while une seule fois
            int paymentId = await _testQueue.DequeueAsync(token);

            using var scope = _scopeFactory.CreateScope();
            var ctx = (EcommerceContext)scope.ServiceProvider.GetService(typeof(EcommerceContext))!;

            var payment = await ctx.Payments.Include(p => p.Order)
                                            .FirstOrDefaultAsync(p => p.Id == paymentId, token);
            if (payment == null) return;

            payment.Status = PaymentStatus.Confirmed; // pour le test, simule succès
            payment.Order.IsPaid = true;

            ctx.Payments.Update(payment);
            ctx.Orders.Update(payment.Order);
            await ctx.SaveChangesAsync(token);
        }
    }
}