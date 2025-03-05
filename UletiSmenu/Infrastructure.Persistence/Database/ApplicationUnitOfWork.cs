using Core.Services;

namespace Infrastructure.Persistence.Database;
public class ApplicationUnitOfWork : IApplicationUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public ApplicationUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CommitTransactionAsync() =>
        await _context.Database.CommitTransactionAsync();

    public async Task BeginTransactionAsync() =>
        await _context.Database.BeginTransactionAsync();

    public async Task RollbackTransactionAsync() =>
        await _context.Database.RollbackTransactionAsync();

    public async Task SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
