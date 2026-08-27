using Core.Models.Entities;
using Core.Models.ValueObjects;
using UletiSmenu.Tests.TestHelpers;

namespace UletiSmenu.Tests.Domain;

public class EmployerBillingTests
{
    [Fact]
    public void GrantRegistrationBonus_DoesNotStackOnTheSameInstance()
    {
        var employer = CreateEmployer();

        employer.GrantRegistrationBonus(5);
        employer.GrantRegistrationBonus(5);

        Assert.Equal(5, employer.PostCredits);
    }

    private static Employer CreateEmployer()
    {
        return Employer.Create(
            Guid.NewGuid(),
            "Restoran",
            "employer@test.com",
            "employer@test.com",
            "0610000000",
            string.Empty,
            HelperMethods.EnsureSuccess(PIB.Create("123456789")),
            HelperMethods.EnsureSuccess(MB.Create("87654321")),
            null,
            null,
            null,
            Address.Empty()).Value;
    }
}
