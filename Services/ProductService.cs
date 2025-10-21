using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ILogger<ProductService> _logger;
        private readonly IMemoryCache _cache;

        // Durée de vie du cache (configurable)
        private readonly TimeSpan _absoluteExpiration = TimeSpan.FromMinutes(5);  // max 5 min
        private readonly TimeSpan _slidingExpiration = TimeSpan.FromMinutes(2);   // prolongé si utilisé

        // Clé de version (permet d’invalider tous les caches produits d’un coup)
        private const string CacheVersionKey = "products_version";

        public ProductService(IProductRepository repo, ILogger<ProductService> logger, IMemoryCache cache)
        {
            _repo = repo;
            _logger = logger;
            _cache = cache;
        }


        // ======== CRUD logique métier

        // Recherche ...tous les produits avec pagination + filtres
        public async Task<PagedResult<Product>> GetAllAsync(ProductQueryParams query)
        {
            // Pour éviter abus côté client
            if (query.PageNumber < 1) query.PageNumber = 1;
            if (query.PageSize <= 0) query.PageSize = 10;
            if (query.PageSize > 100) query.PageSize = 100;

            // Récupère version du cache
            var version = _cache.Get<int?>(CacheVersionKey) ?? 0;

            // Génère une clé unique pour ce jeu de paramètres
            var cacheKey =
                $"products_v{version}::{query.Search}_{query.Category}_{query.MinPrice}_{query.MaxPrice}_{query.InStock}_{query.SortBy}_{query.Descending}_{query.PageNumber}_{query.PageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResult<Product>? cachedResult))
            {
                _logger.LogInformation("Cache HIT pour {CacheKey}", cacheKey);
                return cachedResult!;
            }

            _logger.LogInformation("Cache MISS pour {CacheKey}, requête DB...", cacheKey);

            var items = await _repo.GetAllAsync(query);
            var total = await _repo.CountAsync(query);

            var result = new PagedResult<Product>
            {
                Items = items,
                TotalCount = total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };

            // Stocke le résultat dans le cache
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(_absoluteExpiration)
                .SetSlidingExpiration(_slidingExpiration);

            _cache.Set(cacheKey, result, options);

            return result;
        }

        // Recherche ...un produit par son Id
        public async Task<Product?> GetByIdAsync(int id)
        {
            var version = _cache.Get<int?>(CacheVersionKey) ?? 0;
            var cacheKey = $"product_v{version}_{id}";

            if (_cache.TryGetValue(cacheKey, out Product? cached))
            {
                _logger.LogInformation("Cache HIT pour produit Id={ProductId}", id);
                return cached!;
            }

            _logger.LogInformation("Cache MISS pour produit Id={ProductId}", id);
            var product = await _repo.GetByIdAsync(id);

            if (product != null)
            {
                _cache.Set(cacheKey, product, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _absoluteExpiration,
                    SlidingExpiration = _slidingExpiration
                });
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

            var added = await _repo.AddAsync(product);

            // Invalide le cache global
            InvalidateProductCache();

            return added;
        }

        // Modifie ...un produit
        public async Task<Product?> UpdateAsync(Product product)
        {
            _logger.LogInformation("Modification du produit Id={ProductId}", product.Id);

            var updated = await _repo.UpdateAsync(product);

            if (updated == null)
            {
                _logger.LogWarning("Impossible de mettre à jour le produit Id={ProductId}", product.Id);
            }
            else
            {
                InvalidateProductCache();
            }

            return updated;
        }

        // Supprime ...un produit par son Id
        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Suppression du produit Id={ProductId}", id);

            var deleted = await _repo.DeleteAsync(id);

            if (!deleted)
            {
                _logger.LogWarning("Impossible de supprimer le produit Id={ProductId}", id);
            }
            else
            {
                InvalidateProductCache();
            }

            return deleted;
        }

        // Compte les produits selon filtres (pagination)
        public async Task<int> CountAsync(ProductQueryParams query)
        {
            _logger.LogInformation(
                "Comptage des produits avec filtres: Search={Search}, Category={Category}, MinPrice={MinPrice}, MaxPrice={MaxPrice}, InStock={InStock}",
                query.Search, query.Category, query.MinPrice, query.MaxPrice, query.InStock
            );

            return await _repo.CountAsync(query);
        }



        // ========== Méthode d’invalidation du cache global
        private void InvalidateProductCache()
        {
            // Incrémente le numéro de version global → toutes les clés deviennent obsolètes
            var version = _cache.Get<int?>(CacheVersionKey) ?? 0;
            _cache.Set(CacheVersionKey, version + 1);

            _logger.LogInformation("Cache produit invalidé (version {OldVersion} → {NewVersion})", version, version + 1);
        }
    }
}