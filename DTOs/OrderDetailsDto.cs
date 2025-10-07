using EcommerceApi.Models;

namespace EcommerceApi.DTOs

{
    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime Date { get; set; }
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}
