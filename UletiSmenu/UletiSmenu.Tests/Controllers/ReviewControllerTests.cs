using API.Controllers;
using Core.DTOs;
using Core.Services;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace UletiSmenu.Tests.Controllers;

public class ReviewControllerTests
{
    [Fact]
    public async Task EmployeeReviewPage_ForbidsUnrelatedAuthenticatedUser()
    {
        var reviewService = new Mock<IReviewService>();
        var controller = CreateController(reviewService.Object, Guid.NewGuid(), "Employee");

        var result = await controller.GetEmployeeReviewPage(Guid.NewGuid());

        Assert.IsType<ForbidResult>(result);
        reviewService.Verify(
            service => service.GetEmployeeReviewPageAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task EmployeeReviewPage_AllowsEmployeeToReadOwnReviews()
    {
        var employeeId = Guid.NewGuid();
        var reviewService = new Mock<IReviewService>();
        reviewService
            .Setup(service => service.GetEmployeeReviewPageAsync(employeeId))
            .ReturnsAsync(Result.Success(new ReviewPageDTO()));
        var controller = CreateController(reviewService.Object, employeeId, "Employee");

        var result = await controller.GetEmployeeReviewPage(employeeId);

        Assert.IsType<OkObjectResult>(result);
    }

    private static ReviewController CreateController(
        IReviewService reviewService,
        Guid userId,
        string role)
    {
        var controller = new ReviewController(reviewService, Mock.Of<IUserService>());
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            },
            authenticationType: "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }
}
