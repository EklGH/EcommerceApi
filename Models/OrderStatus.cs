namespace EcommerceApi.Models
{
    public enum OrderStatus                     // enum et non class
    {
        Pending,    // En attente
        Shipped,    // Expédiée
        Delivered,  // Livrée
        Cancelled   // Annulée
    }
}
