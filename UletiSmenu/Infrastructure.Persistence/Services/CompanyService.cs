using Core.Models.Entities;
using Core.Models.ValueObjects;
using Core.Repositories;
using Core.Services;
using CSharpFunctionalExtensions;
using System.ComponentModel.Design;

namespace Infrastructure.Persistence.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ICompanyRepository _companyRepository;
        private readonly IUserRepository _userRepository;

        public CompanyService(ICompanyRepository companyRepository, IUserRepository userRepository)
        {
            _companyRepository = companyRepository;
            _userRepository = userRepository;
        }

        public Task<IEnumerable<Company>> GetAllCompaniesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Company?> GetCompanyByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Company?> GetCompanyByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> CreateCompanyAsync(Guid companyId, string name, Address address)
        {
            var existingCompany = await _companyRepository.FindAsync(c => c.Id == companyId);
            if (existingCompany.Any()) return Result.Failure("Company already exists.");

            var companyResult = Company.Create(companyId, name, address);
            if (companyResult.IsFailure) return Result.Failure(companyResult.Error);

            var company = companyResult.Value;

            await _companyRepository.AddAsync(company);
            return Result.Success(company);
        }


        public Task<Company> UpdateCompanyAsync(Company company)
        {
            throw new NotImplementedException();
        }

        public Task<Company> DeleteCompanyAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Company> GetCompanyByCityAsync(string city)
        {
            throw new NotImplementedException();
        }
    }
}
