namespace API.DTOs
{
    public record RegisterEmployeeDTO(string FirstName, string LastName, string Email, string UserName, string Password, string PhoneNumber, string ProfilePhoto, PIB PIB, MB MB);
}
