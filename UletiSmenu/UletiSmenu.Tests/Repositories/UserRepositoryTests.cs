using Core.Models.Entities;
using Core.Models.ValueObjects;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Repositories
{
    public class UserRepositoryTests : IAsyncLifetime
    {
        private ApplicationDbContext _context = null!;
        private UserManager<User> _userManager = null!;
        private UserRepository _repository = null!;
        private int _employerSequence;

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

            _repository = new UserRepository(_context, _userManager);
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
            _userManager.Dispose();
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldExcludeDeletedEmployers()
        {
            var visible = await CreateEmployerAsync("Visible Restoran", "Beograd");
            var deleted = await CreateEmployerAsync("Deleted Restoran", "Beograd");
            deleted.MarkDeletedTombstone(DateTime.UtcNow);
            await _context.SaveChangesAsync();

            var (items, totalCount) = await _repository.GetEmployerDirectoryPagedAsync(null, null, 1, 10);

            Assert.Equal(1, totalCount);
            Assert.Equal(visible.Id, Assert.Single(items).Id);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldMatchBranchCityCaseInsensitive()
        {
            var matching = await CreateEmployerAsync("Branch Match", "Beograd");
            await CreateEmployerAsync("Other City", "Niš");
            await AddLocationAsync(matching.Id, "Novi Sad");

            var (items, totalCount) = await _repository.GetEmployerDirectoryPagedAsync("novi sad", null, 1, 10);

            Assert.Equal(1, totalCount);
            Assert.Equal(matching.Id, Assert.Single(items).Id);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldMatchEmployerAddressCity()
        {
            var matching = await CreateEmployerAsync("Address Match", "Subotica");
            await CreateEmployerAsync("Other Address", "Beograd");

            var (items, totalCount) = await _repository.GetEmployerDirectoryPagedAsync("SUBOTICA", null, 1, 10);

            Assert.Equal(1, totalCount);
            Assert.Equal(matching.Id, Assert.Single(items).Id);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldNotMatchGeographyCityForFilter()
        {
            SeedGeography("802824", "Novi Sad");
            var employer = await CreateEmployerAsync("Geo Only", "Beograd");
            employer.SetGeographyCodes("RS", "89010", "802824");
            await _context.SaveChangesAsync();

            var (noviSadItems, noviSadCount) = await _repository.GetEmployerDirectoryPagedAsync("Novi Sad", null, 1, 10);
            var (beogradItems, beogradCount) = await _repository.GetEmployerDirectoryPagedAsync("Beograd", null, 1, 10);

            Assert.Equal(0, noviSadCount);
            Assert.Empty(noviSadItems);
            Assert.Equal(1, beogradCount);
            Assert.Equal(employer.Id, Assert.Single(beogradItems).Id);
        }

        [Fact]
        public async Task GetEmployerDirectoryPagedAsync_ShouldSearchByNameAndPage()
        {
            await CreateEmployerAsync("Alpha Grill", "Beograd");
            await CreateEmployerAsync("Alpha Cafe", "Beograd");
            await CreateEmployerAsync("Beta Bistro", "Beograd");

            var (pageOne, totalCount) = await _repository.GetEmployerDirectoryPagedAsync(null, "alpha", 1, 1);
            var (pageTwo, _) = await _repository.GetEmployerDirectoryPagedAsync(null, "alpha", 2, 1);

            Assert.Equal(2, totalCount);
            Assert.Equal("Alpha Cafe", Assert.Single(pageOne).Name);
            Assert.Equal("Alpha Grill", Assert.Single(pageTwo).Name);
        }

        private async Task<Employer> CreateEmployerAsync(string name, string cityName)
        {
            _employerSequence++;
            var street = HelperMethods.EnsureSuccess(Street.Create("Ulica", "1"));
            var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("11000"));
            var country = HelperMethods.EnsureSuccess(Country.Create("Srbija"));
            var region = HelperMethods.EnsureSuccess(Region.Create("Beograd"));
            var city = HelperMethods.EnsureSuccess(City.Create(cityName, postalCode, country, region));
            var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

            var employer = HelperMethods.EnsureSuccess(Employer.Create(
                Guid.NewGuid(),
                name,
                $"employer{_employerSequence}@test.com",
                $"employer{_employerSequence}@test.com",
                $"06100000{_employerSequence:00}",
                string.Empty,
                HelperMethods.EnsureSuccess(PIB.Create("123456789")),
                HelperMethods.EnsureSuccess(MB.Create("12345678")),
                null,
                null,
                null,
                address));
            employer.SetPublicSlug($"employer-{_employerSequence}");

            var create = await _userManager.CreateAsync(employer, "Password1!");
            Assert.True(create.Succeeded, string.Join("; ", create.Errors.Select(error => error.Description)));
            return employer;
        }

        private async Task AddLocationAsync(Guid employerId, string city)
        {
            var location = HelperMethods.EnsureSuccess(RestaurantLocation.Create(
                Guid.NewGuid(),
                employerId,
                "Lokacija",
                "060111222",
                "123456789",
                "12345678",
                "Ulica",
                "1",
                city,
                "11000",
                "Srbija",
                "Beograd",
                "RS",
                "89010",
                "802824"));

            _context.RestaurantLocations.Add(location);
            await _context.SaveChangesAsync();
        }

        private void SeedGeography(string cityCode, string cityName)
        {
            _context.GeographyCountries.Add(new GeographyCountry("RS", "Serbia", "Srbija"));
            _context.GeographyRegions.Add(new GeographyRegion("89010", "RS", "Vojvodina", "Vojvodina"));
            _context.GeographyCities.Add(new GeographyCity(cityCode, "89010", cityName, cityName));
            _context.SaveChanges();
        }
    }
}
