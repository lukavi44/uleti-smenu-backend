namespace API.DTOs
{
    public record ProfilePhotoUploadDTO(Guid UserId, IFormFile File);
}
