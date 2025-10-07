using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }


        // Création d'un paiement (statut initial : Pending)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("Token invalide ou manquant pour la création de paiement");
                return Unauthorized(new { message = "Token invalide ou manquant (aucun userId)" });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Token invalide : userId incorrect ({UserIdClaim})", userIdClaim.Value);
                return Unauthorized(new { message = "Token invalide : userId incorrect" });
            }

            try
            {
                var payment = await _paymentService.CreatePaymentAsync(userId, dto);
                var result = new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Status = payment.Status.ToString(),
                    CreatedAt = payment.CreatedAt,
                    ProcessedAt = payment.ProcessedAt
                };

                _logger.LogInformation(
                    "Paiement créé avec succès : PaymentId {PaymentId}, UserId {UserId}, OrderId {OrderId}, Amount {Amount}",
                    result.Id, userId, result.OrderId, result.Amount);

                return AcceptedAtAction(nameof(GetPayment), new { id = result.Id }, result);     // 202 Accepted
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du paiement pour UserId {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        // Récupérer un paiement par ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _paymentService.GetPaymentAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Paiement non trouvé : PaymentId {PaymentId}", id);
                return NotFound(new { message = "Paiement introuvable" });
            }

            var result = new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                Amount = payment.Amount,
                Status = payment.Status.ToString(),
                CreatedAt = payment.CreatedAt,
                ProcessedAt = payment.ProcessedAt
            };

            _logger.LogInformation("Paiement récupéré : PaymentId {PaymentId}, UserId {UserId}", id, payment.OrderId);
            return Ok(result);
        }
    }
}
