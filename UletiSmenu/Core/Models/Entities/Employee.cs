using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Employee : User
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public ICollection<Application>? Applications { get; private set; } = new List<Application>();
        //CV
        //Rating
        //numberOfAngazmans

        public Employee() : base () {}

        private Employee(Guid id, string email, string username, string phoneNumber, string profilePhoto,
            ICollection<Application> applications, string firstName, string lastName) : base(id, email, username, phoneNumber, profilePhoto)
        {
            Applications = applications;
            FirstName = firstName;
            LastName = lastName;
        }

        public static Result<Employee> Create(Guid id, string email, string username, string phoneNumber, string profilePhoto,
            ICollection<Application> applications, string firstName, string lastName)
        {
            var userResult = User.Create(id, email, username, phoneNumber, profilePhoto);
            if (userResult.IsFailure)
                return Result.Failure<Employee>(userResult.Error);

            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<Employee>("First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<Employee>("Last name cannot be empty.");

            var employee = new Employee(id, email, username, phoneNumber, profilePhoto, applications, firstName, lastName);
            return Result.Success(employee);
        }
    }
}
