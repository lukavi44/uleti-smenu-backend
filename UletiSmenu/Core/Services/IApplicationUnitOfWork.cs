namespace Core.Services
{
    public interface IApplicationUnitOfWork
    {
        public Task CommitTransactionAsync();
        public Task BeginTransactionAsync();
        public Task RollbackTransactionAsync();
        public Task SaveChangesAsync();
    }
}
