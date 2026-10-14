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
        var user = await SeedUser(db, "pera", "stara123");
        var oldHash = user.Password;

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.ChangePassword(user.Id, new ChangePasswordDto { OldPassword = "pogresna", NewPassword = "nova12345" }));

        Assert.Equal(oldHash, (await db.Users.FindAsync(user.Id))!.Password);
    }

    [Fact]
    public async Task ChangePassword_Valid_StoresNewHash()
    {
        var (svc, db) = Create();
        var user = await SeedUser(db, "pera", "stara123");

        await svc.ChangePassword(user.Id, new ChangePasswordDto { OldPassword = "stara123", NewPassword = "nova12345" });

        var saved = await db.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("nova12345", saved!.Password));
        Assert.False(BCrypt.Net.BCrypt.Verify("stara123", saved.Password));
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsersWithoutPasswords()
    {
        var (svc, db) = Create();
        await SeedUser(db, "pera", "x12345678");
        await SeedUser(db, "mika", "y12345678");

        var result = svc.GetUsers();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, u => u.Username == "pera");
        Assert.Contains(result, u => u.Username == "mika");
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
    public async Task ChangeRole_Valid_UpdatesRole()
    {
        var (svc, db) = Create();
        var user = await SeedUser(db, "pera", "x12345678", UserTypes.Admin);
        var otherRole = Enum.GetValues<UserTypes>().First(r => r != UserTypes.Admin);

        await svc.ChangeRole(500, user.Id, otherRole);

        Assert.Equal(otherRole, (await db.Users.FindAsync(user.Id))!.Role);
    }
}