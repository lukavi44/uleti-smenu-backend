namespace API.DTOs
{
    public record JobPostDTO(string Title, string Description, string Position, string Status, int Salary, DateTime StartingDate);
}