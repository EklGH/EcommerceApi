namespace EcommerceApi.Models
{
    public class OrderItem             // order item = article de commande
    {
        public int Id { get; set; }
        public int OrderId { get; set; }            // relation : Clé étrangère vers Order.Id
        public int ProductId { get; set; }          // relation Clé étrangère Product.Id
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Order? Order { get; set; }            // relation vers Order.
        public Product? Product { get; set; }        // relation vers Product.
    }
}
