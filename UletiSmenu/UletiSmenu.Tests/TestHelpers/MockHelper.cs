using Core.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace UletiSmenu.Tests.TestHelpers
{
    public static class MockHelper
    {
        public static Mock<UserManager<User>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        public static Mock<SignInManager<User>> CreateSignInManagerMock()
        {
            var userManager = CreateUserManagerMock();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }
    }
}
