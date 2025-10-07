using EcommerceApi.DTOs;
using EcommerceApi.Models;

namespace EcommerceApi.Services
{
    public interface IPaymentService
    {
        Task<Payment> CreatePaymentAsync(int userId, CreatePaymentDto dto);
        Task<Payment?> GetPaymentAsync(int paymentId);
    }
}
