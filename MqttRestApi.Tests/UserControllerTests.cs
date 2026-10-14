using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MQTTRestApi.Controllers;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Services;

namespace MqttRestApi.Tests;

public class UserControllerTests
{
    private static (UserController ctrl, Mock<IAuthService> auth, Mock<IUsersService> users, Mock<ITokenService> tokens)
        Create(int userId = 1)
    {
        var auth = new Mock<IAuthService>();
        var users = new Mock<IUsersService>();
        var tokens = new Mock<ITokenService>();
        tokens.Setup(t => t.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
              .Returns("jwt-token");

        var ctrl = new UserController(auth.Object, users.Object).WithUser(userId);
        return (ctrl, auth, users, tokens);
    }

    private static object? TokenOf(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return ok.Value!.GetType().GetProperty("token")!.GetValue(ok.Value);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var (ctrl, auth, _, tokens) = Create();
        auth.Setup(a => a.Login("pera", "tajna123"))
            .ReturnsAsync(new UserInfoDto { Id = 4, Username = "pera", Role = UserTypes.Admin });

        var result = await ctrl.Login(new LoginDto("pera", "tajna123"), tokens.Object);

        Assert.Equal("jwt-token", TokenOf(result));
        tokens.Verify(t => t.CreateToken("4", "pera", UserTypes.Admin.ToString()), Times.Once);
    }

    [Fact]
    public async Task Login_ServiceThrows_PropagatesException()
    {
        var (ctrl, auth, _, tokens) = Create();
        auth.Setup(a => a.Login(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("losi podaci"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctrl.Login(new LoginDto("x", "y"), tokens.Object));
    }

    [Fact]
    public async Task Register_ValidData_ReturnsToken()
    {
        var (ctrl, auth, _, tokens) = Create();
        auth.Setup(a => a.Register(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserTypes>()))
            .ReturnsAsync(new UserInfoDto { Id = 9, Username = "pera", Role = UserTypes.Admin });

        var result = await ctrl.Register(
            new RegisterDto("pera", "tajna123"), tokens.Object);

        Assert.Equal("jwt-token", TokenOf(result));
        tokens.Verify(t => t.CreateToken("9", "pera", UserTypes.Admin.ToString()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_UsesUserIdFromClaims()
    {
        var (ctrl, _, users, _) = Create(userId: 42);
        var dto = new ChangePasswordDto { OldPassword = "stara", NewPassword = "nova12345" };

        var result = await ctrl.ChangePassword(dto);

        users.Verify(u => u.ChangePassword(42, dto), Times.Once);
        Assert.Equal("Sifra uspesno promenjena.", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task ChangeRole_CallsService()
    {
        var (ctrl, _, users, _) = Create();

        var result = await ctrl.ChangeRole(5, UserTypes.Admin);

        users.Verify(u => u.ChangeRole(1, 5, UserTypes.Admin), Times.Once);
        Assert.Equal("Role uspesno promenjen.", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public void ListUsers_ReturnsUsersFromService()
    {
        var (ctrl, _, users, _) = Create();
        var list = new List<UserInfoDto> { new() { Id = 1, Username = "pera", Role = UserTypes.Admin } };
        users.Setup(u => u.GetUsers()).Returns(list);

        var result = ctrl.ListUsers();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Theory]
    [InlineData(nameof(UserController.ChangeRole))]
    [InlineData(nameof(UserController.ListUsers))]
    public void AdminEndpoints_RequireAdminRole(string method)
    {
        var attrs = typeof(UserController).GetMethod(method)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();

        Assert.Contains(attrs, a => a.Roles == "Admin");
    }
}