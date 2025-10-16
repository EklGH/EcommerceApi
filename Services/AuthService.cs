using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EcommerceApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly EcommerceContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, EcommerceContext context, ILogger<AuthService> logger)         // DI
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }



        // ======== REGISTER (création utilisateur)
        public async Task<ServiceResponse<User>> RegisterAsync(string username, string email, string password, string role = "Client")
        {
            var response = new ServiceResponse<User>();

            if (await _context.Users.AnyAsync(u => u.Email == email))   // Vérifier si email existe dans la DB
            {
                _logger.LogWarning("Tentative de création d'utilisateur avec email déjà utilisé: {Email}", email);
                response.Success = false;
                response.Message = "Email déjà utilisé";
                return response;
            }

            var user = new User                                         // Création du nouvel utilisateur
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Création d'un nouvel utilisateur: {Username} ({Email}) avec rôle {Role}", username, email, role);

            response.Data = user;
            response.Message = "Utilisateur créé avec succès.";
            return response;
        }



        // ======== REGISTER (création admin)
        public async Task<ServiceResponse<User>> RegisterAdminAsync(string username, string email, string password)
        {
            _logger.LogInformation("Création d'un nouvel admin: {Username} ({Email})", username, email);
            return await RegisterAsync(username, email, password, "Admin");
        }



        // ======== HASH/ VERIFY PASSWORD
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }



        // ======== LOGIN
        public async Task<ServiceResponse<string>> LoginAsync(string email, string password)
        {
            var response = new ServiceResponse<string>();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Échec de connexion pour l'email: {Email}", email);
                response.Success = false;
                response.Message = "Email ou mot de passe incorrect";
                return response;
            }

            response.Data = GenerateJwtToken(user);                     // générer JWT
            response.Message = "Connexion réussie";
            _logger.LogInformation("Connexion réussie pour l'utilisateur: {Email}", email);

            return response;
        }



        // ======== GENERATION JWT
        public string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var keyString = _configuration["JwtSettings:Key"]
                ?? throw new ArgumentNullException("JwtSettings:Key", "La clé JWT est manquante dans la configuration.");

            var durationString = _configuration["JwtSettings:DurationInMinutes"]
                ?? throw new ArgumentNullException("JwtSettings:DurationInMinutes", "La durée JWT est manquante dans la configuration.");

            var key = Encoding.ASCII.GetBytes(keyString);
            var duration = int.Parse(durationString);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]                   // claims Id, Email, Role
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddMinutes(duration),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}