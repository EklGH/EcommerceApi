using EcommerceApi.DTOs;
using EcommerceApi.Models;

namespace EcommerceApi.Services
{
    public interface IProductService
    {
        Task<PagedResult<Product>> GetAllAsync(ProductQueryParams query);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product?> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync(ProductQueryParams query);
    }
}
