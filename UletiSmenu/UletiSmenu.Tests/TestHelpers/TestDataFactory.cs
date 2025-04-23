using Core.Models.Entities;
using Core.Models.ValueObjects;

namespace UletiSmenu.Tests.TestHelpers
{
    public class TestDataFactory
    {
        public static Employer CreateFakeRegisterEmployer()
        {
            var street = HelperMethods.EnsureSuccess(Street.Create("Main Street", "42A"));
            var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("12345"));
            var country = HelperMethods.EnsureSuccess(Country.Create("TestCountry"));
            var region = HelperMethods.EnsureSuccess(Region.Create("TestRegion"));
            var city = HelperMethods.EnsureSuccess(City.Create("Testville", postalCode, country, region));
            
            var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

            var employerResult = Employer.Create(
                id: new Guid(),
                name: "Test Employer Ltd",
                email: "testemployer@example.com",
                username: "testemployer",
                phoneNumber: "0601112223",
                pib: HelperMethods.EnsureSuccess(PIB.Create("123456789")),
                mb: HelperMethods.EnsureSuccess(MB.Create("87654321")),
                profilePhoto: "",
                subscriptionId: Guid.NewGuid(),
                subscriptionStart: DateTime.UtcNow,
                subscriptionStop: DateTime.UtcNow.AddYears(1),
                address: address
            ).Value;

            return employerResult;
        }

        //public static Employee CreateFakeRegisterEmployee()
        //{
        //    var street = HelperMethods.EnsureSuccess(Street.Create("Main Street", "42A"));
        //    var postalCode = HelperMethods.EnsureSuccess(PostalCode.Create("12345"));
        //    var country = HelperMethods.EnsureSuccess(Country.Create("TestCountry"));
        //    var region = HelperMethods.EnsureSuccess(Region.Create("TestRegion"));
        //    var city = HelperMethods.EnsureSuccess(City.Create("Testville", postalCode, country, region));

        //    var address = HelperMethods.EnsureSuccess(Address.Create(street, city));

        //}
    }
}
