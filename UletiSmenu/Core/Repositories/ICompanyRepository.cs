using Core.Models.Entities;
using Core.Models.Enums;

namespace Core.Repositories
{
    public interface ICompanyRepository : IRepository<Company>
    {
        Task<IEnumerable<Company>> GetCompaniesByCity(string city);
    }
}
