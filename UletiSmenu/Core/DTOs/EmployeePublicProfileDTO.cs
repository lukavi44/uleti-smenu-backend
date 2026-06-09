namespace Core.DTOs
{
    public class EmployeePublicProfileDTO
    {
        public Guid EmployeeId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
        public List<WorkExperienceDTO> WorkExperiences { get; set; } = new();
        public List<EmployeePlatformShiftDTO> PlatformShifts { get; set; } = new();
    }
}
