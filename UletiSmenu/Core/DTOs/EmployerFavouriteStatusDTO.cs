namespace Core.DTOs
{
    public class EmployerFavouriteStatusDTO
    {
        public Guid EmployerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public bool IsFavourite { get; set; }
    }
}
