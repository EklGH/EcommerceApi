using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }                       // utile pour GET/PUT/DELETE

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = null!;

        [Range(0.01, 1000000)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = null!;
    }
}
