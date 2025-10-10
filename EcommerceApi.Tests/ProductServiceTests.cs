using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Repositories;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class ProductServiceTests
    {
        private EcommerceContext? _context;
        private ProductService? _service;


        // Méthode exécutée avant chaque test : initialise une base de données InMemory + service de produits.
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EcommerceContext>()    // Configuration InMemory
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            var repository = new ProductRepository(_context);
            var logger = new Mock<ILogger<ProductService>>();                // Mock Logger

            _service = new ProductService(repository, logger.Object);
        }


        // Test 1 : vérifie que GetAllAsync() retourne tous les produits enregistrés
        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllProducts()
        {
            // Arrange — on ajoute quelques produits en base
            _context!.Products.Add(new Product { Name = "P1", Price = 10, Stock = 1 });
            _context.Products.Add(new Product { Name = "P2", Price = 20, Stock = 2 });
            _context.Products.Add(new Product { Name = "P3", Price = 30, Stock = 3 });
            await _context.SaveChangesAsync();

            // Act — on appelle la méthode avec des paramètres simples
            var result = await _service!.GetAllAsync(
                pageNumber: 1,
                pageSize: 10,
                search: null,
                category: null,
                minPrice: null,
                maxPrice: null,
                sort: null
            );

            // Assert — on vérifie que les 3 produits sont bien retournés
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Exists(p => p.Name == "P1"));
            Assert.IsTrue(result.Exists(p => p.Name == "P2"));
            Assert.IsTrue(result.Exists(p => p.Name == "P3"));
        }

        // Test 2 : vérifie que GetByIdAsync() retourne le bon produit après insertion.
        [TestMethod]
        public async Task GetByIdAsync_ShouldReturnProduct()
        {
            var product = new Product { Name = "Test2", Price = 20, Stock = 2 };
            _context!.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = await _service!.GetByIdAsync(product.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Test2", result.Name);
        }

        // Test 3 : vérifie que AddAsync() ajoute bien un produit en base.
        [TestMethod]
        public async Task AddAsync_ShouldAddProduct()
        {
            var product = new Product { Name = "Test", Price = 10, Stock = 5 };

            var added = await _service!.AddAsync(product);

            Assert.IsNotNull(added);
            Assert.AreEqual("Test", added.Name);
        }

        // Test 4 : vérifie que UpdateAsync() modifie bien les infos d’un produit existant.
        [TestMethod]
        public async Task UpdateAsync_ShouldModifyProduct()
        {
            var product = new Product { Name = "OldName", Price = 15, Stock = 3 };
            _context!.Products.Add(product);
            await _context.SaveChangesAsync();

            product.Name = "NewName";
            var updated = await _service!.UpdateAsync(product);

            Assert.IsNotNull(updated);
            Assert.AreEqual("NewName", updated.Name);
        }

        // Test 5 : vérifie que DeleteAsync() supprime bien un produit existant.
        [TestMethod]
        public async Task DeleteAsync_ShouldRemoveProduct()
        {
            var product = new Product { Name = "ToDelete", Price = 5, Stock = 1 };
            _context!.Products.Add(product);
            await _context.SaveChangesAsync();

            var result = await _service!.DeleteAsync(product.Id);

            Assert.IsTrue(result);
            var check = await _service.GetByIdAsync(product.Id);
            Assert.IsNull(check);
        }

        // Test 6 : vérifie que CountAsync() retourne le nombre correct de produits.
        [TestMethod]
        public async Task CountAsync_ShouldReturnCorrectCount()
        {
            _context!.Products.Add(new Product { Name = "P1", Price = 10, Stock = 1 });
            _context.Products.Add(new Product { Name = "P2", Price = 20, Stock = 2 });
            await _context.SaveChangesAsync();

            var count = await _service!.CountAsync(null, null, null, null);
            Assert.AreEqual(2, count);
        }

        
    }
}
