using EcommerceApi.Models;

namespace EcommerceApi.Services
{
    public interface IAuthService
    {
        Task<ServiceResponse<User>> RegisterAsync(string username, string email, string password, string role = "Client");
        Task<ServiceResponse<User>> RegisterAdminAsync(string username, string email, string password);
        Task<ServiceResponse<string>> LoginAsync(string email, string password);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        string GenerateJwtToken(User user);
    }
}
