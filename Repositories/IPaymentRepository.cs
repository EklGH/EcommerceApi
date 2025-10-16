using EcommerceApi.Models;

namespace EcommerceApi.Repositories
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByIdempotencyKeyAsync(string key);
        Task AddAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
