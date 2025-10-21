using EcommerceApi.Data;
using EcommerceApi.DTOs;
using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Intrinsics.X86;

namespace EcommerceApi.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly EcommerceContext _context;

        public ProductRepository(EcommerceContext context)
        {
            _context = context;
        }


        // ======== CRUD

        // Recherche ...tous les produits avec pagination + filtres + tri
        public async Task<List<Product>> GetAllAsync(ProductQueryParams query)
        {
            var q = _context.Products.AsQueryable();

            // Recherche insensible à la casse
            string? search = query.Search?.ToLower();
            string? category = query.Category?.ToLower();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(p => p.Name.ToLower().Contains(search));

            if (!string.IsNullOrEmpty(category))
                q = q.Where(p => p.Category.ToLower() == category);

            if (query.MinPrice.HasValue)
                q = q.Where(p => p.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                q = q.Where(p => p.Price <= query.MaxPrice.Value);

            if (query.InStock.HasValue && query.InStock.Value)
                q = q.Where(p => p.Stock > 0);

            // Tri dynamique
            if (!string.IsNullOrEmpty(query.SortBy))
            {
                q = (query.SortBy.ToLower(), query.Descending) switch
                {
                    ("name", true) => q.OrderByDescending(p => p.Name),
                    ("name", false) => q.OrderBy(p => p.Name),
                    ("price", true) => q.OrderByDescending(p => p.Price),
                    ("price", false) => q.OrderBy(p => p.Price),
                    ("stock", true) => q.OrderByDescending(p => p.Stock),
                    ("stock", false) => q.OrderBy(p => p.Stock),
                    _ => q.OrderBy(p => p.Name)
                };
            }
            else
            {
                q = q.OrderBy(p => p.Name);
            }

            // Pagination
            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        // Recherche ...un produit par son Id
        public async Task<Product?> GetByIdAsync(int id) => await _context.Products.FindAsync(id);

        // Ajoute ...un nouveau produit
        public async Task<Product> AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        // Modifie ...un produit
        public async Task<Product?> UpdateAsync(Product product)
        {
            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null) return null;

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.Price = product.Price;
            existing.Stock = product.Stock;
            existing.Category = product.Category;

            await _context.SaveChangesAsync();
            return existing;
        }

        // Supprime ...un produit par son Id
        public async Task<bool> DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }


        // ======== Utilitaires

        // Compte les produits selon filtres (pagination)
        public async Task<int> CountAsync(ProductQueryParams query)
        {
            var q = _context.Products.AsQueryable();

            // Recherche insensible à la casse
            string? search = query.Search?.ToLower();
            string? category = query.Category?.ToLower();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(p => p.Name.ToLower().Contains(search));

            if (!string.IsNullOrEmpty(category))
                q = q.Where(p => p.Category.ToLower() == category);

            if (query.MinPrice.HasValue)
                q = q.Where(p => p.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                q = q.Where(p => p.Price <= query.MaxPrice.Value);

            if (query.InStock.HasValue && query.InStock.Value)
                q = q.Where(p => p.Stock > 0);

            return await q.CountAsync();
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}