using Microsoft.EntityFrameworkCore;
using Moq;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

public class AuthServiceTests
{
    private static (AuthService service, AppDbContext db, Mock<ILoggerService> logger) Create()
    {
        var db = TestHelpers.CreateDb();
        var logger = new Mock<ILoggerService>();
        return (new AuthService(db, logger.Object), db, logger);
    }

    private static async Task SeedUser(AppDbContext db, string username, string password, UserTypes role = UserTypes.Admin)
    {
        db.Users.Add(new User(username, BCrypt.Net.BCrypt.HashPassword(password), role));
        await db.SaveChangesAsync();
    }
    
    [Fact]
    public async Task Register_NewUser_SavesUserWithHashedPassword()
    {
        var (service, db, logger) = Create();

        var result = await service.Register("test", "test1234", UserTypes.Admin);

        Assert.Equal("test", result.Username);
        Assert.Equal(UserTypes.Admin, result.Role);

        var saved = await db.Users.SingleAsync();
        Assert.NotEqual("test1234", saved.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("test1234", saved.Password));
        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsBadRequest()
    {
        var (service, db, _) = Create();
        await SeedUser(db, "test", "test1234");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.Register("test", "test2345", UserTypes.Admin));

        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsUserInfo()
    {
        var (service, db, logger) = Create();
        await SeedUser(db, "test", "test1234", UserTypes.Admin);

        var result = await service.Login("test", "test1234");

        Assert.Equal("test", result.Username);
        Assert.Equal(UserTypes.Admin, result.Role);
        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsBadRequestAndLogs()
    {
        var (service, db, logger) = Create();
        await SeedUser(db, "test", "test1234");

        await Assert.ThrowsAsync<BadRequestException>(() => service.Login("test", "testaaaaa"));

        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsBadRequest()
    {
        var (service, _, _) = Create();

        await Assert.ThrowsAsync<BadRequestException>(() => service.Login("ne postoji", "bilo sta"));
    }
}