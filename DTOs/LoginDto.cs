using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        public required string Password { get; set; }
    }
}
