namespace Core.DTOs
{
    public class EmployerFavouriteStatusDTO
    {
        public Guid EmployerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public string PublicSlug { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsFavourite { get; set; }
    }
}
