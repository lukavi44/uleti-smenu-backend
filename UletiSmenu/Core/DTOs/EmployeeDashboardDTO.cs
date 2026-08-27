namespace Core.DTOs
{
    public class EmployeeDashboardDTO
    {
        public int ApplicationCount { get; set; }
        public int AcceptedShiftCount { get; set; }
        public int TotalEarnings { get; set; }
        public EmployeeApplicationDTO? NextShift { get; set; }
        public List<EmployeeApplicationDTO> AcceptedShifts { get; set; } = new();
    }
}
