using EcommerceApi.Data;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly EcommerceContext _context;

        public PaymentRepository(EcommerceContext context)
        {
            _context = context;
        }

        public Task<Payment?> GetByIdAsync(int id)
            => _context.Payments.FirstOrDefaultAsync(p => p.Id == id);

        public Task<Payment?> GetByIdempotencyKeyAsync(string key)
            => _context.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == key);

        public async Task AddAsync(Payment payment)
        {
            await _context.Payments.AddAsync(payment);
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
