using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Repositories;
using EcommerceApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class ProductServiceTests
    {
        private EcommerceContext? _context;
        private ProductService? _service;
        private IMemoryCache? _memoryCache;



        // ======== Initialisation avant chaque test

        [TestInitialize]
        public void Setup()
        {
            // Création d'une base de données InMemory unique pour chaque test
            var options = new DbContextOptionsBuilder<EcommerceContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            // Création du repository et du service avec un logger mocké
            var repository = new ProductRepository(_context);
            var logger = new Mock<ILogger<ProductService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _service = new ProductService(repository, logger.Object, _memoryCache);
        }



        // ======== CRUD Tests

        // Test la création d'un produit
        [TestMethod]
        public async Task AddAsync_ShouldAddProduct()
        {
            var product = new Product { Name = "Test", Price = 10, Stock = 5 };
            var added = await _service!.AddAsync(product);

            Assert.IsNotNull(added);
            Assert.AreEqual("Test", added.Name);
        }

        // Test la recherche d'un produit par son Id
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

        // Test la modification d'un produit existant
        [TestMethod]
        public async Task UpdateAsync_ShouldModifyProduct()
        {
            var product = new Product { Name = "OldName", Price = 15, Stock = 3 };
            _context!.Products.Add(product);
            await _context.SaveChangesAsync();

            product.Name = "NewName";
            var updated = await _service!.UpdateAsync(product);

            Assert.IsNotNull(updated);           // Vérifie que le produit a bien été mis à jour
            Assert.AreEqual("NewName", updated.Name);
        }

        // Test la suppression d'un produit
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



        // ======== Count Test

        // Vérifie que CountAsync retourne le nombre exact de produits avec ou sans filtre
        [TestMethod]
        public async Task CountAsync_ShouldReturnCorrectCount()
        {
            _context!.Products.Add(new Product { Name = "P1", Price = 10, Stock = 1 });
            _context.Products.Add(new Product { Name = "P2", Price = 20, Stock = 2 });
            await _context.SaveChangesAsync();

            var query = new ProductQueryParams();           // Pas de filtre
            var count = await _service!.CountAsync(query);
            Assert.AreEqual(2, count);                      // Devrait retourner 2, le nombre exact de produits ajoutés
        }



        // ======== GetAllAsync Tests (pagination, filtre, tri)

        // Vérifie que GetAllAsync retourne tous les produits
        [TestMethod]
        public async Task GetAllAsync_ShouldReturnAllProducts()
        {
            _context!.Products.Add(new Product { Name = "P1", Price = 10, Stock = 1 });
            _context.Products.Add(new Product { Name = "P2", Price = 20, Stock = 2 });
            _context.Products.Add(new Product { Name = "P3", Price = 30, Stock = 3 });
            await _context.SaveChangesAsync();

            var query = new ProductQueryParams { PageNumber = 1, PageSize = 10 };
            var result = await _service!.GetAllAsync(query);

            // Vérifie que tous les produits sont retournés et correspondent aux noms attendus
            Assert.IsNotNull(result);
            Assert.AreEqual(3, result.Items.Count);
            Assert.IsTrue(result.Items.Any(p => p.Name == "P1"));
            Assert.IsTrue(result.Items.Any(p => p.Name == "P2"));
            Assert.IsTrue(result.Items.Any(p => p.Name == "P3"));
        }

        // Vérifie que la pagination fonctionne correctement
        [TestMethod]
        public async Task GetAllAsync_ShouldApplyPagination()
        {
            // Ajoute 5 produits
            for (int i = 1; i <= 5; i++)
                _context!.Products.Add(new Product { Name = $"P{i}", Price = i * 10, Stock = i });
            await _context!.SaveChangesAsync();

            var query = new ProductQueryParams { PageNumber = 2, PageSize = 2 };
            var result = await _service!.GetAllAsync(query);

            Assert.AreEqual(2, result.Items.Count);                           // Devrait retourner 2 produits pour la page 2
            Assert.AreEqual(5, result.TotalCount);                            // Vérifie le total de produits correspond à 5           
        }

        // Vérifie que le filtre de recherche fonctionne
        [TestMethod]
        public async Task GetAllAsync_ShouldFilterBySearch()
        {
            _context!.Products.Add(new Product { Name = "Laptop", Price = 1000, Stock = 5 });
            _context.Products.Add(new Product { Name = "Mouse", Price = 20, Stock = 10 });
            await _context.SaveChangesAsync();

            var query = new ProductQueryParams { Search = "Laptop" };
            var result = await _service!.GetAllAsync(query);

            Assert.AreEqual(1, result.Items.Count);                              // Devrait retourner uniquement le produit "Laptop"
            Assert.AreEqual("Laptop", result.Items[0].Name);
        }

        // Vérifie que le tri descendant par prix fonctionne
        [TestMethod]
        public async Task GetAllAsync_ShouldSortDescendingByPrice()
        {
            _context!.Products.Add(new Product { Name = "P1", Price = 10, Stock = 1 });
            _context.Products.Add(new Product { Name = "P2", Price = 50, Stock = 1 });
            _context.Products.Add(new Product { Name = "P3", Price = 30, Stock = 1 });
            await _context.SaveChangesAsync();

            var query = new ProductQueryParams { SortBy = "Price", Descending = true };
            var result = await _service!.GetAllAsync(query);

            Assert.AreEqual(50, result.Items.First().Price);                      // Vérifie que le produit le plus cher est en premier
            Assert.AreEqual(10, result.Items.Last().Price);                       // Vérifie que le produit le moins cher est en dernier
        }

        // Vérifie que le filtre par intervalle de prix fonctionne
        [TestMethod]
        public async Task GetAllAsync_ShouldFilterByPriceRange()
        {
            _context!.Products.Add(new Product { Name = "Cheap", Price = 5, Stock = 1 });
            _context.Products.Add(new Product { Name = "Medium", Price = 20, Stock = 1 });
            _context.Products.Add(new Product { Name = "Expensive", Price = 50, Stock = 1 });
            await _context.SaveChangesAsync();

            var query = new ProductQueryParams { MinPrice = 10, MaxPrice = 30 };
            var result = await _service!.GetAllAsync(query);

            // Devrait retourner uniquement le produit dont le prix est compris entre 10 et 30
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual("Medium", result.Items[0].Name);
        }

        // ======== Caching Test
        [TestMethod]
        public async Task GetByIdAsync_ShouldUseCache_AfterFirstCall()
        {
            // Arrange : ajoute un produit dans la DB
            var product = new Product { Name = "CachedProduct", Price = 100, Stock = 10 };
            _context!.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act : premier appel (devrait venir de la DB)
            var firstCall = await _service!.GetByIdAsync(product.Id);

            // Supprime le produit de la DB pour simuler une suppression côté base
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Act : deuxième appel (devrait venir du cache)
            var secondCall = await _service.GetByIdAsync(product.Id);

            // Assert : le produit est toujours disponible (cache)
            Assert.IsNotNull(firstCall);
            Assert.IsNotNull(secondCall);
            Assert.AreEqual(firstCall!.Name, secondCall!.Name);
        }
    }
}
