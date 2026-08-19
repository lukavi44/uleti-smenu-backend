using Core.Models.Entities;
using Core.Models.Enums;
using Core.Models.ValueObjects;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Repositories
{
    public class JobPostRepositoryTests
    {
        [Fact]
        public async Task GetDirectoryActiveJobCountsByEmployerIdsAsync_ShouldUseDirectoryLifecycleNotStatusActiveOnly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);
            var employer = CreateEmployer("Counted Employer");
            var otherEmployer = CreateEmployer("Other Employer");
            var location = CreateLocation(employer.Id, "Beograd");
            var otherLocation = CreateLocation(otherEmployer.Id, "Niš");
            var startingDate = DateTime.UtcNow.AddHours(5);

            context.Users.AddRange(employer, otherEmployer);
            context.RestaurantLocations.AddRange(location, otherLocation);
            context.JobPosts.AddRange(
                CreateJobPost(employer.Id, location.Id, JobStatusEnum.Draft, startingDate),
                CreateJobPost(employer.Id, location.Id, JobStatusEnum.Active, startingDate),
                CreateJobPost(employer.Id, location.Id, JobStatusEnum.Cancelled, startingDate),
                CreateJobPost(employer.Id, location.Id, JobStatusEnum.Expired, startingDate),
                CreateJobPost(otherEmployer.Id, otherLocation.Id, JobStatusEnum.Active, startingDate));
            await context.SaveChangesAsync();

            var repository = new JobPostRepository(context);
            var counts = await repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                new[] { employer.Id },
                DateTime.UtcNow);

            Assert.Equal(2, counts[employer.Id]);
            Assert.False(counts.ContainsKey(otherEmployer.Id));
            Assert.Equal(1, await repository.CountActiveByEmployerIdAsync(employer.Id));
        }

        [Fact]
        public async Task GetDirectoryActiveJobCountsByEmployerIdsAsync_ShouldExcludePostsPastArchiveWindow()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);
            var employer = CreateEmployer("Window Employer");
            var location = CreateLocation(employer.Id, "Beograd");
            var startingDate = DateTime.UtcNow.AddHours(5);

            context.Users.Add(employer);
            context.RestaurantLocations.Add(location);
            context.JobPosts.Add(CreateJobPost(employer.Id, location.Id, JobStatusEnum.Active, startingDate));
            await context.SaveChangesAsync();

            var repository = new JobPostRepository(context);
            var counts = await repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                new[] { employer.Id },
                startingDate.AddHours(2));

            Assert.Empty(counts);
        }

        [Fact]
        public async Task GetDirectoryActiveJobCountsByEmployerIdsAsync_ShouldReturnEmpty_WhenNoIds()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ApplicationDbContext(options);
            var repository = new JobPostRepository(context);

            var counts = await repository.GetDirectoryActiveJobCountsByEmployerIdsAsync(
                Array.Empty<Guid>(),
                DateTime.UtcNow);

            Assert.Empty(counts);
        }

        private static Employer CreateEmployer(string name)
        {
            var street = HelperMethods.EnsureSuccess(Street.Create("Ulica", "1"));
            var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("11000"));
            var country = HelperMethods.EnsureSuccess(Country.Create("Srbija"));
            var region = HelperMethods.EnsureSuccess(Region.Create("Beograd"));
            var city = HelperMethods.EnsureSuccess(City.Create("Beograd", postalCode, country, region));
            var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

            var employer = HelperMethods.EnsureSuccess(Employer.Create(
                Guid.NewGuid(),
                name,
                $"{Guid.NewGuid():N}@test.com",
                "employer",
                $"06{Random.Shared.Next(10000000, 99999999)}",
                string.Empty,
                HelperMethods.EnsureSuccess(PIB.Create("123456789")),
                HelperMethods.EnsureSuccess(MB.Create("12345678")),
                null,
                null,
                null,
                address));
            employer.SetPublicSlug(Guid.NewGuid().ToString("N")[..12]);
            return employer;
        }

        private static RestaurantLocation CreateLocation(Guid employerId, string city)
        {
            return HelperMethods.EnsureSuccess(RestaurantLocation.Create(
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
        }

        private static JobPost CreateJobPost(
            Guid employerId,
            Guid locationId,
            JobStatusEnum status,
            DateTime startingDate)
        {
            return HelperMethods.EnsureSuccess(JobPost.Create(
                Guid.NewGuid(),
                "Konobar",
                "Smenski rad",
                status,
                startingDate,
                startingDate,
                employerId,
                locationId,
                5000,
                "Konobar"));
        }
    }
}
