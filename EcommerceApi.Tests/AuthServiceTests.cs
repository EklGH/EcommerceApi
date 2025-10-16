using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using EcommerceApi.Data;
using EcommerceApi.Models;
using EcommerceApi.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceApi.Tests
{
    [TestClass]
    public class AuthServiceTests
    {
        private EcommerceContext? _context;
        private AuthService? _authService;


        // Méthode exécutée avant chaque test : initialise une base de données InMemory + service d'authentification.
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EcommerceContext>()    // Config InMemory
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EcommerceContext(options);

            var inMemorySettings = new Dictionary<string, string?>           // Mock de IConfiguration (JWT)
            {
                {"JwtSettings:Key", "supersecretkeyfortest123456789ABCDEF"},
                {"JwtSettings:Issuer", "testIssuer"},
                {"JwtSettings:Audience", "testAudience"},
                {"JwtSettings:DurationInMinutes", "60"}
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var logger = new Mock<ILogger<AuthService>>();                   // Mock Logger

            _authService = new AuthService(configuration, _context, logger.Object);
        }


        // Test 1 : Inscription réussie d’un utilisateur client
        [TestMethod]
        public async Task RegisterAsync_ShouldCreateUser()
        {
            var response = await _authService!.RegisterAsync("John", "john@test.com", "password");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual("john@test.com", response.Data.Email);
        }

        // Test 2 : Inscription échoue si email déjà utilisé
        [TestMethod]
        public async Task RegisterAsync_ShouldFail_IfEmailExists()
        {
            await _authService!.RegisterAsync("Jane", "jane@test.com", "password");
            var response = await _authService.RegisterAsync("Jane2", "jane@test.com", "password");

            Assert.IsFalse(response.Success);
            Assert.AreEqual("Email déjà utilisé", response.Message);
        }

        // Test 3 : Inscription d’un admin avec rôle Admin
        [TestMethod]
        public async Task RegisterAdminAsync_ShouldCreateAdmin()
        {
            var response = await _authService!.RegisterAdminAsync("AdminUser", "admin@test.com", "adminpwd");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Data);
            Assert.AreEqual("Admin", response.Data.Role);
        }

        // Test 4 : Vérifie que HashPassword + VerifyPassword fonctionnent
        [TestMethod]
        public void HashPassword_And_VerifyPassword_ShouldWork()
        {
            var hashed = _authService!.HashPassword("secret");
            var result = _authService.VerifyPassword("secret", hashed);

            Assert.IsTrue(result);
        }

        // Test 5 : Login réussi avec bons identifiants
        [TestMethod]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
        {
            await _authService!.RegisterAsync("John", "john@test.com", "mypassword");

            var response = await _authService.LoginAsync("john@test.com", "mypassword");

            Assert.IsTrue(response.Success);
            Assert.IsNotNull(response.Data);
            Assert.IsTrue(response.Data.Length > 10); // token non vide
        }

        // Test 6 : Login échoue si mauvais identifiants
        [TestMethod]
        public async Task LoginAsync_ShouldFail_WhenCredentialsInvalid()
        {
            await _authService!.RegisterAsync("John", "john@test.com", "mypassword");

            var response = await _authService.LoginAsync("john@test.com", "wrongpassword");

            Assert.IsFalse(response.Success);
            Assert.AreEqual("Email ou mot de passe incorrect", response.Message);
        }

        // Test 7 : Génération de token JWT valide
        [TestMethod]
        public void GenerateJwtToken_ShouldReturnValidToken()
        {
            var user = new User
            {
                Id = 1,
                Email = "test@test.com",
                Username = "Test",
                Role = "Client"
            };

            var token = _authService!.GenerateJwtToken(user);

            Assert.IsNotNull(token);
            Assert.IsTrue(token.Length > 20);
        }
    }
}
