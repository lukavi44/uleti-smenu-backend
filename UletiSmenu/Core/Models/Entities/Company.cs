using Core.Models.ValueObjects;
using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Company
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Address Address { get; private set; }
        public ICollection<JobPost> Posts { get; private set; } = new List<JobPost>();

        private Company()
        {
        }

        private Company(Guid id, string name, Address address)
        {
            Id = id;
            Name = name;
            Address = address;
        }

        public static Result<Company> Create(Guid id, string name, Address address)
        {
            if (id == Guid.Empty)
            {
                return Result.Failure<Company>("ID cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(name))
                return Result.Failure<Company>("Name cannot be empty.");

            return Result.Success(new Company(id, name, address));
        }
    }
}
