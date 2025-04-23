using Core.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace API.RequestHelper
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<User>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<User> userManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, optionsAccessor)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
        {
            var identity = await base.GenerateClaimsAsync(user);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim("userId", user.Id.ToString()));
            identity.AddClaim(new Claim("email", user.Email ?? ""));

            return identity;
        }
    }
}
