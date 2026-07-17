using API.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace UletiSmenu.Tests.Controllers
{
    public class UserControllerTests
    {
        [Theory]
        [InlineData(nameof(UserController.GetAllUsers), "Admin")]
        [InlineData(nameof(UserController.GetUsersByRole), "Employee,Admin")]
        public void SensitiveDirectoryEndpointsRequireExpectedRoles(
            string methodName,
            string expectedRoles)
        {
            var method = typeof(UserController).GetMethod(methodName);

            Assert.NotNull(method);
            var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorize);
            Assert.Equal(expectedRoles, authorize!.Roles);
        }

        [Fact]
        public void ProfilePhotoUploadHasRequestSizeLimits()
        {
            var method = typeof(UserController).GetMethod(nameof(UserController.UpdateMyProfilePhoto));

            Assert.NotNull(method);
            var requestLimit = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
            var formLimit = method.GetCustomAttribute<RequestFormLimitsAttribute>();

            Assert.NotNull(requestLimit);
            Assert.NotNull(formLimit);
            Assert.Equal(6 * 1024 * 1024, formLimit!.MultipartBodyLengthLimit);
        }
    }
}
