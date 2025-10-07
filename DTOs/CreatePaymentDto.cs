using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class CreatePaymentDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal Amount { get; set; }

        public string? IdempotencyKey { get; set; }
    }
}
