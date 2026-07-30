using API.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace UletiSmenu.Tests.Controllers;

public class AdminControllerTests
{
    [Fact]
    public void AdminControllerRequiresAdminRole()
    {
        var authorize = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Admin", authorize!.Roles);
    }

    [Theory]
    [InlineData(nameof(AdminController.SetEmployerSuspension))]
    [InlineData(nameof(AdminController.SetEmployerAdminNotes))]
    [InlineData(nameof(AdminController.SetEmployerVerification))]
    public void EmployerWriteEndpointsExistOnAdminController(string methodName)
    {
        var method = typeof(AdminController).GetMethod(methodName);

        Assert.NotNull(method);
    }
}
