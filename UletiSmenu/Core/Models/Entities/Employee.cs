using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class Employee : User
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string? City { get; private set; }
        public DateTime? DateOfBirth { get; private set; }
        //public ICollection<Employer> FavouriteEmployers { get; private set; } = new List<Employer>();
        public string? CvFileName { get; private set; }
        public ICollection<Application>? Applications { get; private set; } = new List<Application>();
        //HistoryOfJobsOnThisSite --> This should be a whole new table probably

        public Employee() : base () {}

        private Employee(Guid id, string email, string username, string? phoneNumber, string? profilePhoto,
            ICollection<Application> applications, string firstName, string lastName, string? cvFileName) : base(id, email, username, phoneNumber, profilePhoto)
        {
            Applications = applications;
            FirstName = firstName;
            LastName = lastName;
            CvFileName = cvFileName;
        }

        public static Result<Employee> Create(Guid id, string email, string username, string phoneNumber, string profilePhoto,
            ICollection<Application> applications, string firstName, string lastName, string? cvFileName = null)
        {
            var userResult = User.Create(id, email, username, phoneNumber, profilePhoto);
            if (userResult.IsFailure)
                return Result.Failure<Employee>(userResult.Error);

            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure<Employee>("First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure<Employee>("Last name cannot be empty.");

            var employee = new Employee(id, email, username, phoneNumber, profilePhoto, applications, firstName, lastName, cvFileName);
            return Result.Success(employee);
        }

        public static Result<Employee> CreateMinimal(Guid id, string email, string username)
        {
            var userResult = User.Create(id, email, username, null, null);
            if (userResult.IsFailure)
                return Result.Failure<Employee>(userResult.Error);

            return Result.Success(new Employee(id, email, username, null, null, new List<Application>(), string.Empty, string.Empty, null));
        }

        public Result UpdateProfile(string firstName, string lastName, string phoneNumber, string? city)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                return Result.Failure("First name cannot be empty.");

            if (string.IsNullOrWhiteSpace(lastName))
                return Result.Failure("Last name cannot be empty.");

            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Result.Failure("Phone number cannot be empty.");

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            PhoneNumber = phoneNumber.Trim();
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

            return Result.Success();
        }

        public void SetDateOfBirth(DateTime? dateOfBirth)
        {
            DateOfBirth = dateOfBirth?.Date;
        }

        //public void AddFavouriteEmployer(Employer employer)
        //{
        //    if (!FavouriteEmployers.Contains(employer))
        //        FavouriteEmployers.Add(employer);
        //}
    }
}
