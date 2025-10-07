using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.DTOs
{
    public class UserRegisterDto
    {
        [Required]
        [MinLength(3)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères.")]
        public required string Password { get; set; }
    }
}
