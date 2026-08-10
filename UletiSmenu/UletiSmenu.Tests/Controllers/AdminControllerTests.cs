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
    [InlineData(nameof(AdminController.GetUsers))]
    [InlineData(nameof(AdminController.SetUserLockout))]
    [InlineData(nameof(AdminController.SetEmployerSuspension))]
    [InlineData(nameof(AdminController.SetEmployerAdminNotes))]
    [InlineData(nameof(AdminController.SetEmployerVerification))]
    [InlineData(nameof(AdminController.GetJobPostDetail))]
    [InlineData(nameof(AdminController.ArchiveJobPost))]
    [InlineData(nameof(AdminController.GetContactMessages))]
    [InlineData(nameof(AdminController.GetContactMessage))]
    [InlineData(nameof(AdminController.ResolveContactMessage))]
    [InlineData(nameof(AdminController.GetReports))]
    [InlineData(nameof(AdminController.GetReport))]
    [InlineData(nameof(AdminController.ResolveReport))]
    public void AdminWriteAndModerationEndpointsExist(string methodName)
    {
        var method = typeof(AdminController).GetMethod(methodName);

        Assert.NotNull(method);
    }
}
