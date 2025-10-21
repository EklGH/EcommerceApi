using EcommerceApi.DTOs;
using EcommerceApi.Models;

namespace EcommerceApi.Repositories
{
    public interface IProductRepository           // le contrat pour ProductRepository
    {
        Task<List<Product>> GetAllAsync(ProductQueryParams query);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product?> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync(ProductQueryParams query);
        Task SaveChangesAsync();
    }
}
