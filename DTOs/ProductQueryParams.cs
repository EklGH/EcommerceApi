namespace EcommerceApi.DTOs
{
    public class ProductQueryParams
    {
        public string? Search { get; set; }
        public string? Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? InStock { get; set; }

        public string? SortBy { get; set; }                  // "price", "name", "stock"
        public bool Descending { get; set; } = false;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
