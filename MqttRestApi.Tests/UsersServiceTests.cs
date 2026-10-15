using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

public class UsersServiceTests
{
    private static (UsersService svc, AppDbContext db) Create()
    {
        var db = TestHelpers.CreateDb();
        return (new UsersService(db), db);
    }

    private static async Task<User> SeedUser(AppDbContext db, string username, string password,
        UserTypes role = UserTypes.Admin)
    {
        var user = new User(username, BCrypt.Net.BCrypt.HashPassword(password), role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task ChangePassword_UnknownUser_ThrowsBadRequest()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.ChangePassword(999, new ChangePasswordDto { OldPassword = "a", NewPassword = "b" }));
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_ThrowsAndKeepsOldHash()
    {
        var (svc, db) = Create();
        var user = await SeedUser(db, "test", "test123");
        var oldHash = user.Password;

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.ChangePassword(user.Id, new ChangePasswordDto { OldPassword = "pogresna", NewPassword = "nova123" }));

        Assert.Equal(oldHash, (await db.Users.FindAsync(user.Id))!.Password);
    }

    [Fact]
    public async Task ChangePassword_Valid_StoresNewHash()
    {
        var (svc, db) = Create();
        var user = await SeedUser(db, "test", "test123");

        await svc.ChangePassword(user.Id, new ChangePasswordDto { OldPassword = "test123", NewPassword = "nova123" });

        var saved = await db.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("nova123", saved!.Password));
        Assert.False(BCrypt.Net.BCrypt.Verify("test123", saved.Password));
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsersWithoutPasswords()
    {
        var (svc, db) = Create();
        await SeedUser(db, "test1", "t12345");
        await SeedUser(db, "test2", "t2345678");

        var result = svc.GetUsers();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Username == "test1");
        Assert.Contains(result, u => u.Username == "test2");
    }

    [Fact]
    public void GetUsers_NoUsers_ReturnsEmptyList()
    {
        var (svc, _) = Create();

        Assert.Empty(svc.GetUsers());
    }

    [Fact]
    public async Task ChangeRole_UnknownUser_ThrowsBadRequest()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<NotFoundException>(() => svc.ChangeRole(1, 999, UserTypes.Admin));
    }

    [Fact]
    public async Task ChangeRole_AdminChangesHisRole_ThrowsBadRequest()
    {
        var (svc, _) = Create();

        await Assert.ThrowsAsync<NotFoundException>(() => svc.ChangeRole(1, 1, UserTypes.Admin));
    }
    
    [Fact]
    public async Task ChangeRole_Valid_UpdatesRole()
    {
        var (svc, db) = Create();
        var user = await SeedUser(db, "test1", "t12345", UserTypes.Admin);
        var otherRole = Enum.GetValues<UserTypes>().First(r => r != UserTypes.Admin);

        await svc.ChangeRole(500, user.Id, otherRole);

        Assert.Equal(otherRole, (await db.Users.FindAsync(user.Id))!.Role);
    }
}