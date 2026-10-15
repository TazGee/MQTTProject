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
        changePasswordDto.OldPassword = changePasswordDto.OldPassword.Trim();
        changePasswordDto.NewPassword = changePasswordDto.NewPassword.Trim();
        
        var user = await dbContext.Users.FindAsync(userId);
        if (user == null) throw new BadRequestException($"Korisnik sa ovim ID-em ne postoji! [id: {userId}]");
        
        ValidatePasswods(changePasswordDto.OldPassword,  changePasswordDto.NewPassword, user.Password);

        string oldPass = user.Password;
        user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            user.Password = oldPass;
            throw new BadRequestException($"Neuspesno cuvanje u bazu podataka, probajte kasnije! {e.Message}");
        }
    }
    
    public List<UserInfoDto> GetUsers()
    {
        var users = dbContext.Users.ToList();

        var userDtos = users.OrderByDescending(u => u.Id)
                           .Select(u => new UserInfoDto
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
        
        if(!Enum.IsDefined(typeof(UserTypes), role)) throw new BadRequestException("Vrednost role-a nije validna!");
        
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

    void ValidatePasswods(string oldPassword, string newPassword, string passHash)
    {
        if(newPassword.Length < 8) throw new BadRequestException("Nova sifra mora sadrzati makar 8 karaktera! (razmaci se brisu sa pocetka i kraja)");
        if(newPassword.Length > 24) throw new BadRequestException("Nova sifra mora biti kraca od 24 karaktera!");
        if(newPassword.Any(ch => ch == '\\')) throw new BadRequestException("Ne smete koristiti '\\' u sifri!");
        
        if (!BCrypt.Net.BCrypt.Verify(oldPassword, passHash)) throw new BadRequestException("Stara sifra se ne poklapa!");
        if (BCrypt.Net.BCrypt.Verify(newPassword, passHash)) throw new BadRequestException("Nova sifra ne moze biti ista kao stara!");
    }
}