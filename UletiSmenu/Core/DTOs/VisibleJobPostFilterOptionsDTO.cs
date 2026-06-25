namespace Core.DTOs
{
    public class VisibleJobPostFilterOptionsDTO
    {
        public List<string> Cities { get; set; } = [];
        public List<VisibleJobPostLocationOptionDTO> Locations { get; set; } = [];
        public List<string> Positions { get; set; } = [];
        public int? MinSalary { get; set; }
        public int? MaxSalary { get; set; }
    }

    public class VisibleJobPostLocationOptionDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
}
