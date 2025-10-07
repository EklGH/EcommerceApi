using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 1000, ErrorMessage = "La quantité doit être entre 1 et 1000.")]
        public int Quantity { get; set; }
    }
}
