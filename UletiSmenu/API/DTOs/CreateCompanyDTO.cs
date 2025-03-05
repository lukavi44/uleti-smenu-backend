namespace API.DTOs
{
    public record CreateCompanyDTO(Guid CompanyId, string Name, string Street, string City, string PostalCode, string Country, string Region);
}
