using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        [MinLength(1, ErrorMessage = "Une commande doit contenir au moins un produit.")]
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
