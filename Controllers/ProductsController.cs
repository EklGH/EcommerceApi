using EcommerceApi.DTOs;
using EcommerceApi.Models;
using EcommerceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _service;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductService service, ILogger<ProductsController> logger)
        {
            _service = service;
            _logger = logger;
        }


        // ======== CRUD gestion des requêtes http

        // Recherche ...tous les produits avec pagination + filtres
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? pageNumber = 1,
            [FromQuery] int? pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? sort = "nameAsc")         // tri croissant/décroissant
        {
            int currentPage = pageNumber ?? 1;
            int currentSize = pageSize ?? 10;

            var products = await _service.GetAllAsync(currentPage, currentSize, search, category, minPrice, maxPrice, sort);
            var total = await _service.CountAsync(search, category, minPrice, maxPrice);

            _logger.LogInformation("Liste des produits récupérée : {Count} produits trouvés (page {Page}, taille {Size})",
                products.Count, currentPage, currentSize);

            var response = new ServiceResponse<List<Product>>
            {
                Data = products,
                TotalCount = total
            };

            return Ok();

        }

        // Recherche ...un produit par Id
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Produit non trouvé : Id {Id}", id);
                return NotFound();
            }
            _logger.LogInformation("Produit récupéré : Id {Id}, Nom {Name}", product.Id, product.Name);
            return Ok(product);
        }

        // Crée ...un produit (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category
            };

            var created = await _service.AddAsync(product);
            _logger.LogInformation("Produit créé : Id {Id}, Nom {Name}", created.Id, created.Name);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        // Modifie ...un produit (Admin)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            var product = new Product
            {
                Id = id,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                Category = dto.Category
            };

            var updated = await _service.UpdateAsync(product);
            if (updated == null)
            {
                _logger.LogWarning("Échec de la mise à jour : produit Id {Id} introuvable", id);
                return NotFound();
            }
            _logger.LogInformation("Produit mis à jour : Id {Id}, Nom {Name}", updated.Id, updated.Name);
            return Ok(updated);
        }

        // Supprime ...un produit (Admin)
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Échec suppression : produit Id {Id} introuvable", id);
                return NotFound();
            }
            _logger.LogInformation("Produit supprimé : Id {Id}", id);
            return NoContent();
        }
    }
}
