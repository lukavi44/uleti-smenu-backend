using Core.DTOs;

namespace Core.Services
{
    public interface IPlatformStatsService
    {
        Task<PlatformStatsDTO> GetPublicStatsAsync();
    }
}
