using EcommerceApi.Models;

namespace EcommerceApi.Repositories
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(int orderId);
        Task<Order?> GetByIdWithItemsAsync(int orderId);
        Task<List<Order>> GetByUserIdAsync(int userId);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task<bool> ExistsAsync(int orderId);
        Task SaveChangesAsync();
    }
}
