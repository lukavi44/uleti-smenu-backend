namespace API.DTOs
{
    public record ResetPasswordDTO(string Email, string Token, string Password);
}
