using EcommerceApi.DTOs;
using EcommerceApi.Models;

namespace EcommerceApi.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(int userId, CreateOrderDto dto);
        Task<List<Order>> GetOrdersByUserAsync(int userId);
        Task<Order> GetOrderByIdAsync(int orderId, int userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task CancelOrderAsync(int orderId, int userId, bool isAdmin = false);
    }
}
