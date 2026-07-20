namespace API.DTOs
{
    public class AdminUserListItemDTO
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
