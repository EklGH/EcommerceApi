using EcommerceApi.DTOs;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // ======== Création d'un client
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(UserRegisterDto dto)
        {
            _logger.LogInformation("Nouvelle tentative d'inscription pour l'email {Email}", dto.Email);

            var user = await _authService.RegisterAsync(dto.Username, dto.Email, dto.Password, "Client");
            if (user == null || user.Data == null)
            {
                _logger.LogWarning("Échec de l'inscription pour l'email {Email}", dto.Email);
                return BadRequest("Erreur lors de l'inscription");
            }
            _logger.LogInformation("Inscription réussie pour l'utilisateur {UserId}", user.Data.Id);
            return Ok(user);
        }

        // ======== Connexion
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            _logger.LogInformation("Tentative de connexion pour l'email {Email}", dto.Email);

            var token = await _authService.LoginAsync(dto.Email, dto.Password);
            if (token == null)
            {
                _logger.LogWarning("Échec de connexion pour l'email {Email}", dto.Email);
                return Unauthorized("Email ou mot de passe incorrect");
            }
            _logger.LogInformation("Connexion réussie pour l'email {Email}", dto.Email);
            return Ok(new { Token = token });
        }

        // ======== Création d'un admin
        [Authorize(Roles = "Admin")]
        [HttpPost("register-admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RegisterAdmin(UserRegisterDto dto)
        {
            _logger.LogInformation("Admin tente de créer un nouvel admin avec l'email {Email}", dto.Email);

            var user = await _authService.RegisterAsync(dto.Username, dto.Email, dto.Password, "Admin");
            if (user == null || user.Data == null)
            {
                _logger.LogWarning("Échec de création d'un admin pour l'email {Email}", dto.Email);
                return BadRequest("Erreur lors de l'inscription Admin");
            }
            _logger.LogInformation("Admin créé avec succès pour l'utilisateur {UserId}", user.Data.Id);
            return Ok(user);
        }
    }
}
