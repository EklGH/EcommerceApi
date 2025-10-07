using System.ComponentModel.DataAnnotations;
using EcommerceApi.Models;

namespace EcommerceApi.DTOs
{
    public class UpdateStatusDto
    {
        [Required]
        public OrderStatus Status { get; set; }
    }
}
