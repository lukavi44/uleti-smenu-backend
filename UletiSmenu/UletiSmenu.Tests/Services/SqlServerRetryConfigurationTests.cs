using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace UletiSmenu.Tests.Services;

public class SqlServerRetryConfigurationTests
{
    [Fact]
    public void Configure_EnablesSqlServerRetryingExecutionStrategy()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=RetryConfig;Trusted_Connection=True",
                SqlServerTransientRetry.Configure)
            .Options;

        using var context = new ApplicationDbContext(options);
        var strategy = context.Database.CreateExecutionStrategy();

        Assert.IsType<SqlServerRetryingExecutionStrategy>(strategy);
    }
}
