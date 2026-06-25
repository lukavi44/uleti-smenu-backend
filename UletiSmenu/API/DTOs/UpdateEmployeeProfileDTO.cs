namespace API.DTOs
{
    public record UpdateEmployeeProfileDTO(
        string FirstName,
        string LastName,
        string PhoneNumber,
        string? City);
}
