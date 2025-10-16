using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }


        // ======== CRUD gestion des requêtes http

        // Crée ...une commande
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                _logger.LogWarning("Impossible de récupérer l’ID utilisateur depuis le token JWT.");
                return Unauthorized("Utilisateur non authentifié ou token invalide.");
            }

            _logger.LogInformation("Utilisateur {UserId} tente de créer une commande avec {ItemCount} articles.",
                userId, dto.Items?.Count ?? 0);

            var order = await _orderService.CreateOrderAsync(userId, dto);
            _logger.LogInformation("Commande {OrderId} créée avec succès pour l’utilisateur {UserId}.",
                order.Id, userId);
            return Ok(order);
        }

        // Recherche...toutes les commandes d'un utilisateur
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyOrders()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                _logger.LogWarning("Impossible de récupérer l’ID utilisateur depuis le token JWT.");
                return Unauthorized("Utilisateur non authentifié ou token invalide.");
            }

            _logger.LogInformation("Utilisateur {UserId} consulte ses commandes.", userId);

            var orders = await _orderService.GetOrdersByUserAsync(userId);
            _logger.LogInformation("{Count} commandes trouvées pour l’utilisateur {UserId}.", orders.Count, userId);
            return Ok(orders);
        }

        // Recherche ...les détails d'une commande par ID
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetOrder(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                _logger.LogWarning("Impossible de récupérer l’ID utilisateur depuis le token JWT.");
                return Unauthorized("Utilisateur non authentifié ou token invalide.");
            }

            _logger.LogInformation("Utilisateur {UserId} consulte la commande {OrderId}.", userId, id);

            var order = await _orderService.GetOrderByIdAsync(id, userId);

            if (order == null)
            {
                _logger.LogWarning("Commande {OrderId} introuvable pour l’utilisateur {UserId}.", id, userId);
                return NotFound($"Commande {id} non trouvée.");
            }
            return Ok(order);
        }

        // Modifie ...le statut d'une commande (Admin)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Enum.IsDefined(typeof(OrderStatus), dto.Status))
            {
                _logger.LogWarning("Tentative de mise à jour de commande {OrderId} avec un statut vide.", id);
                return BadRequest("Le statut ne peut pas être vide.");
            }

            _logger.LogInformation("Admin {AdminId} met à jour le statut de la commande {OrderId} en {Status}.",
                adminId, id, dto.Status);

            await _orderService.UpdateOrderStatusAsync(id, dto.Status);
            _logger.LogInformation("Statut de la commande {OrderId} mis à jour avec succès en {Status}.", id, dto.Status);

            return NoContent();
        }

        // Annule une commande (Client ou Admin)
        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Client,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                _logger.LogWarning("Impossible de récupérer l’ID utilisateur depuis le token JWT.");
                return Unauthorized("Utilisateur non authentifié ou token invalide.");
            }

            var isAdmin = User.IsInRole("Admin");

            try
            {
                await _orderService.CancelOrderAsync(id, userId, isAdmin);
                return Ok(new { message = "Commande annulée avec succès." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erreur lors de l'annulation de la commande {OrderId} : {Message}", id, ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
