using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace API.Security
{
    public static class IdentityEndpointSecurity
    {
        public const string DisabledRegistrationPolicy = "disabled-identity-registration";

        public static void Apply(EndpointBuilder endpointBuilder)
        {
            if (endpointBuilder is RouteEndpointBuilder routeEndpointBuilder
                && string.Equals(
                    routeEndpointBuilder.RoutePattern.RawText?.Trim('/'),
                    "register",
                    StringComparison.OrdinalIgnoreCase))
            {
                endpointBuilder.Metadata.Add(new AuthorizeAttribute(DisabledRegistrationPolicy));
            }
        }
    }
}
