using API.DTOs;
using API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace UletiSmenu.Tests.Controllers;

public class SecurityContractTests
{
    [Fact]
    public void PublicEmployerContractsDoNotExposeSensitiveIdentityOrLegalFields()
    {
        var sensitiveNames = new[]
        {
            "Email",
            "PhoneNumber",
            "PIB",
            "MB",
            "PasswordHash",
            "SecurityStamp",
            "Subscription"
        };

        var publicTypes = new[]
        {
            typeof(EmployerListItemDTO),
            typeof(EmployerPublicSummaryDTO)
        };

        foreach (var publicType in publicTypes)
        {
            var propertyNames = publicType.GetProperties().Select(property => property.Name);
            Assert.Empty(propertyNames.Intersect(sensitiveNames, StringComparer.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void PublicJobPostUsesPublicEmployerSummary()
    {
        var employerProperty = typeof(JobPostPublicDTO).GetProperty(nameof(JobPostPublicDTO.Employer));

        Assert.NotNull(employerProperty);
        Assert.Equal(typeof(EmployerPublicSummaryDTO), employerProperty!.PropertyType);
        Assert.Null(typeof(JobPostPublicDTO).GetProperty("ApplicantCount"));
        Assert.Null(typeof(JobPostPublicDTO).GetProperty("RecentApplicants"));
    }

    [Fact]
    public void GenericIdentityRegistrationEndpointGetsAlwaysDenyPolicy()
    {
        EndpointBuilder endpoint = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/register"),
            order: 0);

        IdentityEndpointSecurity.Apply(endpoint);

        var authorize = Assert.Single(endpoint.Metadata.OfType<AuthorizeAttribute>());
        Assert.Equal(IdentityEndpointSecurity.DisabledRegistrationPolicy, authorize.Policy);
    }

    [Fact]
    public void OtherIdentityEndpointsDoNotGetRegistrationPolicy()
    {
        EndpointBuilder endpoint = new RouteEndpointBuilder(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/login"),
            order: 0);

        IdentityEndpointSecurity.Apply(endpoint);

        Assert.Empty(endpoint.Metadata.OfType<AuthorizeAttribute>());
    }
}
