using Core.Repositories;

namespace Core.Services
{
    public interface IApplicationUnitOfWork
    {
        IFavouriteRepository Favourites { get; }
        INotificationRepository Notifications { get; }
        public Task CommitTransactionAsync();
        public Task BeginTransactionAsync();
        public Task RollbackTransactionAsync();
        public Task SaveChangesAsync();
        public Task<TResult> ExecuteStrategyAsync<TResult>(Func<Task<TResult>> operation);
    }
}
