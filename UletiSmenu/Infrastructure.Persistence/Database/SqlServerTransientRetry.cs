using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Persistence.Database;

public static class SqlServerTransientRetry
{
    public const int MaxRetryCount = 6;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    public static void Configure(SqlServerDbContextOptionsBuilder sqlOptions)
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: MaxRetryCount,
            maxRetryDelay: MaxRetryDelay,
            errorNumbersToAdd: null);
    }
}
