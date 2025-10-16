namespace EcommerceApi.Models
{
    public enum OrderStatus                     // enum et non class
    {
        Pending,    // En attente = 0 (numéro du status sur Postman)
        Shipped,    // Expédiée = 1
        Delivered,  // Livrée = 2
        Cancelled   // Annulée = 3
    }
}
