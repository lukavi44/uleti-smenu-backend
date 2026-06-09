using CSharpFunctionalExtensions;

namespace Core.Models.Entities
{
    public class WorkExperience
    {
        public const int MaxCompanyNameLength = 200;
        public const int MaxPositionLength = 120;
        public const int MaxDescriptionLength = 1000;

        public Guid Id { get; private set; }
        public Guid EmployeeId { get; private set; }
        public string CompanyName { get; private set; } = string.Empty;
        public string Position { get; private set; } = string.Empty;
        public DateTime StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public string? Description { get; private set; }

        private WorkExperience() { }

        public static Result<WorkExperience> Create(
            Guid employeeId,
            string companyName,
            string position,
            DateTime startDate,
            DateTime? endDate,
            string? description)
        {
            if (employeeId == Guid.Empty)
                return Result.Failure<WorkExperience>("Employee ID cannot be empty.");

            var normalizedCompanyName = companyName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedCompanyName))
                return Result.Failure<WorkExperience>("Company name cannot be empty.");

            if (normalizedCompanyName.Length > MaxCompanyNameLength)
                return Result.Failure<WorkExperience>($"Company name cannot exceed {MaxCompanyNameLength} characters.");

            var normalizedPosition = position?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedPosition))
                return Result.Failure<WorkExperience>("Position cannot be empty.");

            if (normalizedPosition.Length > MaxPositionLength)
                return Result.Failure<WorkExperience>($"Position cannot exceed {MaxPositionLength} characters.");

            if (endDate.HasValue && endDate.Value < startDate)
                return Result.Failure<WorkExperience>("End date cannot be before start date.");

            var normalizedDescription = description?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedDescription) && normalizedDescription.Length > MaxDescriptionLength)
                return Result.Failure<WorkExperience>($"Description cannot exceed {MaxDescriptionLength} characters.");

            return Result.Success(new WorkExperience
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                CompanyName = normalizedCompanyName,
                Position = normalizedPosition,
                StartDate = startDate,
                EndDate = endDate,
                Description = string.IsNullOrWhiteSpace(normalizedDescription) ? null : normalizedDescription
            });
        }

        public Result Update(
            string companyName,
            string position,
            DateTime startDate,
            DateTime? endDate,
            string? description)
        {
            var createResult = Create(EmployeeId, companyName, position, startDate, endDate, description);
            if (createResult.IsFailure)
                return Result.Failure(createResult.Error);

            var updated = createResult.Value;
            CompanyName = updated.CompanyName;
            Position = updated.Position;
            StartDate = updated.StartDate;
            EndDate = updated.EndDate;
            Description = updated.Description;

            return Result.Success();
        }
    }
}
