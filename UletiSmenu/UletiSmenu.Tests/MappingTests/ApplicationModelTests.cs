using Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace UletiSmenu.Tests.MappingTests;

public class ApplicationModelTests
{
    [Fact]
    public void ApplicationModel_EnforcesOneApplicationPerEmployeeAndJobPost()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelInspection;Trusted_Connection=True")
            .Options;

        using var context = new ApplicationDbContext(options);
        var application = context.Model.FindEntityType(typeof(Core.Models.Entities.Application));

        Assert.NotNull(application);
        Assert.Contains(
            application!.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { "UserId", "JobPostId" }));
        Assert.Null(application.FindProperty("NumberOfApplicants"));
    }
}
