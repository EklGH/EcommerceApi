namespace EcommerceApi.Models
{
    public class ServiceResponse<T>
    {
        public T? Data { get; set; }
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public int? TotalCount { get; set; }
    }
}
