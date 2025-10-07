using EcommerceApi.Models;

namespace EcommerceApi.Repositories
{
    public interface IProductRepository           // le contrat pour ProductRepository
    {
        Task<List<Product>> GetAllAsync(int pageNumber, int pageSize, string? search, string? category, decimal? minPrice, decimal? maxPrice, string? sort);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> AddAsync(Product product);
        Task<Product?> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync(string? search, string? category, decimal? minPrice, decimal? maxPrice);
    }
}
