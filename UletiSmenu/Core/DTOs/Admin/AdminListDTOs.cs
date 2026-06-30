namespace Core.DTOs.Admin
{
    public class AdminPagedResponseDTO<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AdminCandidateListItemDTO
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? ProfilePhoto { get; set; }
        public int ApplicationsCount { get; set; }
    }

    public class AdminRestaurantListItemDTO
    {
        public Guid Id { get; set; }
        public Guid EmployerId { get; set; }
        public string EmployerName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class AdminJobPostListItemDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ApplicationsCount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class AdminApplicationListItemDTO
    {
        public Guid Id { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string EmployerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime AppliedAtUtc { get; set; }
    }

    public class AdminBillingListItemDTO
    {
        public Guid Id { get; set; }
        public string EmployerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
