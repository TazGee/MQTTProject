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
    private static (AuthService svc, AppDbContext db, Mock<ILoggerService> logger) Create()
    {
        var db = TestHelpers.CreateDb();
        var logger = new Mock<ILoggerService>();
        return (new AuthService(db, logger.Object), db, logger);
    }

    private static async Task SeedUser(AppDbContext db, string username, string password,
        UserTypes role = UserTypes.Admin)
    {
        db.Users.Add(new User(username, BCrypt.Net.BCrypt.HashPassword(password), role));
        await db.SaveChangesAsync();
    }
    
    [Fact]
    public async Task Register_NewUser_SavesUserWithHashedPassword()
    {
        var (svc, db, logger) = Create();

        var result = await svc.Register("pera", "tajna123", UserTypes.Admin);

        Assert.Equal("pera", result.Username);
        Assert.Equal(UserTypes.Admin, result.Role);

        var saved = await db.Users.SingleAsync();
        Assert.NotEqual("tajna123", saved.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify("tajna123", saved.Password));
        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ThrowsBadRequest()
    {
        var (svc, db, _) = Create();
        await SeedUser(db, "pera", "tajna123");

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.Register("pera", "druga", UserTypes.Admin));

        Assert.Equal(1, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsUserInfo()
    {
        var (svc, db, logger) = Create();
        await SeedUser(db, "pera", "tajna123", UserTypes.Admin);

        var result = await svc.Login("pera", "tajna123");

        Assert.Equal("pera", result.Username);
        Assert.Equal(UserTypes.Admin, result.Role);
        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsBadRequestAndLogs()
    {
        var (svc, db, logger) = Create();
        await SeedUser(db, "pera", "tajna123");

        await Assert.ThrowsAsync<BadRequestException>(() => svc.Login("pera", "pogresna"));

        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.AUTH), Times.Once);
    }

    [Fact]
    public async Task Login_UnknownUser_ThrowsBadRequest()
    {
        var (svc, _, _) = Create();

        await Assert.ThrowsAsync<BadRequestException>(() => svc.Login("nepostoji", "bilo sta"));
    }
}