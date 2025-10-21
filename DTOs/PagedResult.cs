namespace EcommerceApi.DTOs
{

    // Résultat paginé avec les éléments et les données de pagination
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();  // Eléments de la page actuelle
        public int TotalCount { get; set; }          // Eléments correspondant aux filtres
        public int PageNumber { get; set; }          // Numéro de la page actuelle
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
