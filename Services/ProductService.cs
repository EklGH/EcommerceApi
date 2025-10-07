using EcommerceApi.Models;
using EcommerceApi.Repositories;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Services
{
    public class ProductService
    {
        private readonly IProductRepository _repo;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repo, ILogger<ProductService> logger)
        {
            _repo = repo;
            _logger = logger;
        }


        // ======== CRUD logique métier

        // Recherche ...tous les produits avec pagination + filtres
        public async Task<List<Product>> GetAllAsync(int pageNumber, int pageSize, string? search, string? category, decimal? minPrice, decimal? maxPrice, string? sort)
        {
            _logger.LogInformation("Récupération de tous les produits - page {PageNumber}, taille {PageSize}", pageNumber, pageSize);
            return await _repo.GetAllAsync(pageNumber, pageSize, search, category, minPrice, maxPrice, sort);
        }

        // Recherche ...un produit par son Id
        public async Task<Product?> GetByIdAsync(int id)
        {
            _logger.LogInformation("Récupération du produit Id={ProductId}", id);
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Produit Id={ProductId} non trouvé", id);
            }
            return product;
        }

        // Ajoute ...un nouveau produit
        public async Task<Product> AddAsync(Product product)
        {
            _logger.LogInformation("Création du produit {ProductName}", product.Name);

            if (product.Price <= 0)
            {
                _logger.LogWarning("Prix invalide pour le produit {ProductName}", product.Name);
                throw new ArgumentException("Le prix doit être supérieur à 0");
            }

            return await _repo.AddAsync(product);
        }

        // Modifie ...un produit
        public async Task<Product?> UpdateAsync(Product product)
        {
            _logger.LogInformation("Modification du produit Id={ProductId}", product.Id);

            var updatedProduct = await _repo.UpdateAsync(product);
            if (updatedProduct == null)
            {
                _logger.LogWarning("Impossible de mettre à jour le produit Id={ProductId} (non trouvé)", product.Id);
            }

            return updatedProduct;
        }

        // Supprime ...un produit par son Id
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Suppression du produit Id={ProductId}", id);
            var result = await _repo.DeleteAsync(id);
            if (!result)
            {
                _logger.LogWarning("Impossible de supprimer le produit Id={ProductId} (non trouvé)", id);
            }
            return result;
        }



        // ======== Utilitaires

        // Compte les produits selon filtres (pagination)
        public async Task<int> CountAsync(string? search, string? category, decimal? minPrice, decimal? maxPrice)
        {
            _logger.LogInformation("Comptage des produits avec filtres: Search={Search}, Category={Category}", search, category);
            return await _repo.CountAsync(search, category, minPrice, maxPrice);
        }
    }
}
