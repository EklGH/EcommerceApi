using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly EcommerceContext _context;

        public OrderRepository(EcommerceContext context)
        {
            _context = context;
        }

        public Task<Order?> GetByIdAsync(int orderId)
            => _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        public Task<Order?> GetByIdWithItemsAsync(int orderId)
            => _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

        public Task<List<Order>> GetByUserIdAsync(int userId)
            => _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        public async Task AddAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public Task<bool> ExistsAsync(int orderId)
            => _context.Orders.AnyAsync(o => o.Id == orderId);

        public Task SaveChangesAsync()
            => _context.SaveChangesAsync();
    }
}
