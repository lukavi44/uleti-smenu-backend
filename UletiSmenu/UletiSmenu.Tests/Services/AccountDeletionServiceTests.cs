using Core.Models;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Models.ValueObjects;
using Core.Services;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Services;

public class AccountDeletionServiceTests : IAsyncLifetime
{
    private ApplicationDbContext _context = null!;
    private UserManager<User> _userManager = null!;
    private Mock<SignInManager<User>> _signInManagerMock = null!;
    private Mock<IFileService> _fileServiceMock = null!;
    private AccountDeletionService _service = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var store = new UserStore<User, IdentityRole<Guid>, ApplicationDbContext, Guid>(_context);
        _userManager = new UserManager<User>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            provider,
            NullLogger<UserManager<User>>.Instance);

        _signInManagerMock = MockHelper.CreateSignInManagerMock();
        _signInManagerMock
            .Setup(manager => manager.SignOutAsync())
            .Returns(Task.CompletedTask);

        _fileServiceMock = new Mock<IFileService>();
        _fileServiceMock
            .Setup(service => service.DeleteImageAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new AccountDeletionService(
            _context,
            _userManager,
            _signInManagerMock.Object,
            _fileServiceMock.Object,
            NullLogger<AccountDeletionService>.Instance);

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        _userManager.Dispose();
    }

    [Fact]
    public async Task DeleteMyAccount_WrongPassword_FailsWithoutMutation()
    {
        var employee = await CreateEmployeeAsync("candidate@test.com", "Password1!");
        var otherEmployer = await CreateEmployerAsync("fav-employer@test.com", "Password1!");
        _context.Favourites.Add(Favourite.Create(employee, otherEmployer).Value);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteMyAccountAsync(employee.Id, "WrongPassword1!");

        Assert.True(result.IsFailure);
        Assert.Contains("Incorrect password", result.Error);
        Assert.False(employee.IsDeleted);
        Assert.Equal(1, await _context.Favourites.CountAsync());
    }

    [Fact]
    public async Task DeleteMyAccount_Candidate_HardDeletesPersonalDataAndAnonymizes()
    {
        var employee = await CreateEmployeeAsync("candidate@test.com", "Password1!");
        employee.UpdateProfilePhoto("/uploads/photo.jpg");
        await _userManager.UpdateAsync(employee);

        var otherEmployer = await CreateEmployerAsync("other-employer@test.com", "Password1!");
        _context.Favourites.Add(Favourite.Create(employee, otherEmployer).Value);
        _context.Notifications.Add(Notification.Create(employee.Id, Guid.NewGuid(), Guid.NewGuid(), "Type", "Body"));
        _context.WorkExperiences.Add(WorkExperience.Create(employee.Id, "Cafe", "Waiter", new DateTime(2024, 1, 1), null, null).Value);
        _context.ConversationReadStates.Add(ConversationReadState.Create(employee.Id, Guid.NewGuid(), DateTime.UtcNow));
        _context.Applications.Add(Application.Create(Guid.NewGuid(), employee.Id, Guid.NewGuid(), ApplicationStatusEnum.Applied, DateTime.UtcNow).Value);
        _context.Applications.Add(Application.Create(Guid.NewGuid(), employee.Id, Guid.NewGuid(), ApplicationStatusEnum.Accepted, DateTime.UtcNow).Value);
        _context.ChatMessages.Add(ChatMessage.Create(Guid.NewGuid(), employee.Id, "Secret PII message", DateTime.UtcNow).Value);
        _context.ContactMessages.Add(ContactMessage.Create(Guid.NewGuid(), "Name", "candidate@test.com", "Subject", "Hello", DateTime.UtcNow).Value);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteMyAccountAsync(employee.Id, "Password1!");

        Assert.True(result.IsSuccess);
        Assert.True(employee.IsDeleted);
        Assert.StartsWith("deleted-", employee.Email, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Deleted", employee.FirstName);
        Assert.Equal("User", employee.LastName);
        Assert.Equal(0, await _context.Favourites.CountAsync());
        Assert.Equal(0, await _context.Notifications.CountAsync());
        Assert.Equal(0, await _context.WorkExperiences.CountAsync());
        Assert.Equal(0, await _context.ConversationReadStates.CountAsync());
        Assert.Equal(0, await _context.Applications.CountAsync(a => a.Status == ApplicationStatusEnum.Applied));
        Assert.Equal(1, await _context.Applications.CountAsync(a => a.Status == ApplicationStatusEnum.Accepted));
        Assert.All(await _context.ChatMessages.ToListAsync(), message => Assert.Equal("[deleted]", message.Content));
        Assert.All(
            await _context.ContactMessages.ToListAsync(),
            message =>
            {
                Assert.Equal("Deleted user", message.Name);
                Assert.Equal("deleted@deleted.local", message.Email);
            });

        _fileServiceMock.Verify(
            service => service.DeleteImageAsync("/uploads/photo.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
        _signInManagerMock.Verify(manager => manager.SignOutAsync(), Times.Once);
        Assert.False(await _userManager.CheckPasswordAsync(employee, "Password1!"));
        Assert.True(await _userManager.IsLockedOutAsync(employee));
    }

    [Fact]
    public async Task DeleteMyAccount_Employer_ArchivesPostsRetainsWalletAndAnonymizesPublicProfile()
    {
        var employer = await CreateEmployerAsync("employer@test.com", "Password1!");
        employer.UpdateProfilePhoto("/uploads/employer.jpg");
        await _userManager.UpdateAsync(employer);

        var starting = DateTime.UtcNow.AddDays(1);
        var post = JobPost.Create(
            Guid.NewGuid(),
            "Title",
            "Description",
            JobStatusEnum.Active,
            starting,
            starting.AddMinutes(30),
            employer.Id,
            Guid.NewGuid(),
            5000,
            "Konobar").Value;
        _context.JobPosts.Add(post);
        _context.WalletTransactions.Add(WalletTransaction.Create(
            Guid.NewGuid(),
            employer.Id,
            100m,
            100m,
            WalletTransactionType.TopUp,
            "Bonus",
            null,
            null,
            null,
            DateTime.UtcNow));
        _context.Favourites.Add(Favourite.Create(await CreateEmployeeAsync("fan@test.com", "Password1!"), employer).Value);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteMyAccountAsync(employer.Id, "Password1!");

        Assert.True(result.IsSuccess);
        Assert.True(employer.IsDeleted);
        Assert.Equal("Deleted employer", employer.Name);
        Assert.StartsWith("deleted-", employer.PublicSlug, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(employer.PIB.Value));
        Assert.Equal(JobStatusEnum.Cancelled, post.Status);
        Assert.Equal(1, await _context.WalletTransactions.CountAsync());
        Assert.Equal(0, await _context.Favourites.CountAsync());
        _fileServiceMock.Verify(
            service => service.DeleteImageAsync("/uploads/employer.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteMyAccount_Idempotent_WhenAlreadyDeleted()
    {
        var employee = await CreateEmployeeAsync("once@test.com", "Password1!");
        var first = await _service.DeleteMyAccountAsync(employee.Id, "Password1!");
        Assert.True(first.IsSuccess);

        var second = await _service.DeleteMyAccountAsync(employee.Id, "anything");
        Assert.True(second.IsSuccess);
        _signInManagerMock.Verify(manager => manager.SignOutAsync(), Times.Once);
    }

    [Fact]
    public void DeleteMyAccount_EndpointRequiresAuthorizeAndRateLimit()
    {
        var method = typeof(API.Controllers.UserController).GetMethod(
            nameof(API.Controllers.UserController.DeleteMyAccount));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false).FirstOrDefault());
        var rateLimit = method.GetCustomAttributes(typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute), false)
            .Cast<Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute>()
            .FirstOrDefault();
        Assert.NotNull(rateLimit);
        Assert.Equal(API.Security.RateLimitPolicies.Contact, rateLimit!.PolicyName);
    }

    private async Task<Employee> CreateEmployeeAsync(string email, string password)
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            email,
            email,
            "0611111111",
            string.Empty,
            new List<Application>(),
            "Ana",
            "Petrovic").Value;

        var create = await _userManager.CreateAsync(employee, password);
        Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(error => error.Description)));
        return employee;
    }

    private async Task<Employer> CreateEmployerAsync(string email, string password)
    {
        var employer = Employer.Create(
            Guid.NewGuid(),
            "Restoran Test",
            email,
            email,
            "0612222222",
            string.Empty,
            HelperMethods.EnsureSuccess(PIB.Create("123456789")),
            HelperMethods.EnsureSuccess(MB.Create("87654321")),
            null,
            null,
            null,
            Address.Empty()).Value;
        employer.SetPublicSlug("restoran-test");

        var create = await _userManager.CreateAsync(employer, password);
        Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(error => error.Description)));
        return employer;
    }
}
