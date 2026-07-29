using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public sealed class ContactMessageDTO
{
    [Required]
    [MinLength(2)]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(160)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MinLength(10)]
    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;
}
