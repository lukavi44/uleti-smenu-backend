using Core.Models.Entities;
using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;

namespace Core.Services
{
    public interface ICompanyService
    {
        Task<IEnumerable<Company>> GetAllCompaniesAsync();
        Task<Company?> GetCompanyByIdAsync(Guid id);
        Task<Company?> GetCompanyByNameAsync(string name);
        Task<Result> CreateCompanyAsync(Guid companyId, string name, Address address);
        Task<Company> UpdateCompanyAsync(Company company);
        Task<Company> DeleteCompanyAsync(Guid id);
        Task<Company> GetCompanyByCityAsync(string city);
    }
}
