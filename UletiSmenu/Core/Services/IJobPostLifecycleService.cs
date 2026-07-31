namespace Core.Services
{
    public interface IJobPostLifecycleService
    {
        /// <summary>
        /// Marks Active job posts past StartingDate+1h as Expired and expires their pending applications.
        /// Returns how many posts were updated.
        /// </summary>
        Task<int> ExpireStaleActivePostsAsync(CancellationToken cancellationToken = default);
    }
}
