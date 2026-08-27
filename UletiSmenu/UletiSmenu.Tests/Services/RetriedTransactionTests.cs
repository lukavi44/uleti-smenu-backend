using Core.Billing;
using Core.Models.Entities;
using Core.Models.ValueObjects;
using Core.Repositories;
using Core.Services;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Services;

public class RetriedTransactionTests
{
    [Fact]
    public async Task OnJobPostCreatedAsync_WhenSaveChangesFailsOnce_DoesNotConsumeCreditAndChargeWallet()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var interceptor = new FailNextSaveChangesInterceptor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection, sqlite => sqlite.ExecutionStrategy(dependencies =>
                new RetryOnceExecutionStrategy(dependencies)))
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var employer = CreateEmployer();
        employer.GrantRegistrationBonus(1);
        employer.CreditWallet(2000m);
        context.Users.Add(employer);
        await context.SaveChangesAsync();

        var userManager = CreateUserManager(context);
        var userRepository = new UserRepository(context, userManager);
        var unitOfWork = new ApplicationUnitOfWork(context);
        var walletLedger = new WalletLedgerService(userRepository, new WalletTransactionRepository(context));
        var jobPostRepository = new Mock<IJobPostRepository>();
        jobPostRepository
            .Setup(repository => repository.CountActiveByEmployerIdAsync(employer.Id))
            .ReturnsAsync(0);
        var billingService = new BillingService(
            userRepository,
            Mock.Of<ISubscriptionRepository>(),
            jobPostRepository.Object,
            new WalletTransactionRepository(context),
            Mock.Of<IPaymentProvider>(),
            walletLedger,
            unitOfWork,
            Options.Create(new BillingSettings { JobPostPrice = 200m }));

        interceptor.FailuresRemaining = 1;
        var jobPostId = Guid.NewGuid();

        var result = await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await unitOfWork.BeginTransactionAsync();
            try
            {
                var chargeResult = await billingService.OnJobPostCreatedAsync(employer.Id, jobPostId);
                if (chargeResult.IsFailure)
                {
                    await unitOfWork.RollbackTransactionAsync();
                    return chargeResult;
                }

                await unitOfWork.CommitTransactionAsync();
                return chargeResult;
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync();
                throw;
            }
        });

        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var persisted = await context.Users.OfType<Employer>().SingleAsync(candidate => candidate.Id == employer.Id);
        Assert.Equal(0, persisted.PostCredits);
        Assert.Equal(2000m, persisted.WalletBalance);
        Assert.Equal(0, await context.WalletTransactions.CountAsync());
    }

    private static Employer CreateEmployer()
    {
        return Employer.Create(
            Guid.NewGuid(),
            "Restoran Test",
            "retry-employer@test.com",
            "retry-employer@test.com",
            "0610000000",
            string.Empty,
            HelperMethods.EnsureSuccess(PIB.Create("123456789")),
            HelperMethods.EnsureSuccess(MB.Create("87654321")),
            null,
            null,
            null,
            Address.Empty()).Value;
    }

    private static UserManager<User> CreateUserManager(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var store = new UserStore<User, IdentityRole<Guid>, ApplicationDbContext, Guid>(context);
        return new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            provider,
            NullLogger<UserManager<User>>.Instance);
    }

    private sealed class FailNextSaveChangesInterceptor : SaveChangesInterceptor
    {
        public int FailuresRemaining { get; set; }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            FailIfRequested();
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            FailIfRequested();
            return ValueTask.FromResult(result);
        }

        private void FailIfRequested()
        {
            if (FailuresRemaining <= 0)
                return;

            FailuresRemaining--;
            throw new InvalidOperationException("simulated transient failure");
        }
    }

    private sealed class RetryOnceExecutionStrategy : ExecutionStrategy
    {
        public RetryOnceExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, maxRetryCount: 1, maxRetryDelay: TimeSpan.Zero)
        {
        }

        protected override bool ShouldRetryOn(Exception exception) =>
            exception is InvalidOperationException;
    }
}
