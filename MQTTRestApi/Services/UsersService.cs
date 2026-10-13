using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

public class UsersService : IUsersService
{
    private readonly AppDbContext dbContext;
    
    public UsersService(AppDbContext dbContext)
    {   
        this.dbContext = dbContext;
    }

    public async Task ChangePassword(int userId, ChangePasswordDto changePasswordDto)
    {
        var user = await dbContext.Users.FindAsync(userId);
        
        if (user == null) throw new BadRequestException($"Korisnik sa ovim ID-em ne postoji! [id: {userId}]");
        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.OldPassword, user.Password)) throw new BadRequestException("Stara sifra se ne poklapa!");
        
        user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            throw new BadRequestException($"Neuspesno cuvanje u bazu podataka, probajte kasnije! {e.Message}");
        }
    }
    
    public List<UserInfoDto> GetUsers()
    {
        var users = dbContext.Users.ToList();

        var userDtos = users.Select(u => new UserInfoDto
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList() 
        ?? throw new BadRequestException($"Neuspesno dobavljanje liste korisnika!");
        
        return userDtos;
    }

    public async Task ChangeRole(int adminId, int userId, UserTypes role)
    {
        if(adminId == userId) throw new NotFoundException("Ne mozete menjati role sami sebi!");
        
        var user = await dbContext.Users.FindAsync(userId);
        
        if (user == null) throw new NotFoundException($"Korisnik sa ovim ID-em ne postoji! [id: {userId}]");
        
        UserTypes oldRole = user.Role;
        user.Role = role;
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            user.Role = oldRole;
            throw new BadRequestException($"Neuspesno cuvanje u bazu podataka, probajte kasnije! {e.Message}");
        }
    }
}