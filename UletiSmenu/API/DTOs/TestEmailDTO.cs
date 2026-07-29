using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public sealed class TestEmailDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
