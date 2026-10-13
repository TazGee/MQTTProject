using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext dbContext;
    private readonly ILoggerService logger;
    
    public AuthService(AppDbContext dbContext, ILoggerService logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }
    
    public async Task<UserInfoDto> Login(string username, string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            await logger.LogMessage($"Neko je probao da se uloguje na {username} i nije uspeo!", LogTypes.AUTH);
            throw new BadRequestException("Username ili password nisu dobri!");
        }
        
        await logger.LogMessage($"Korisnik [{user.Id}]{username} se uspesno ulogovao!", LogTypes.AUTH);
        return new UserInfoDto { Id = user.Id, Username = username, Role = user.Role, CreatedAt = user.CreatedAt };
    }

    public async Task<UserInfoDto> Register(string username, string password, UserTypes TipKorisnika)
    {
        if (await dbContext.Users.AnyAsync(u => u.Username == username))
            throw new BadRequestException("Neko vec koristi ovaj username!");

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var novi = new User(username, hash, TipKorisnika);

        try
        {
            dbContext.Users.Add(novi);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            dbContext.Entry(novi).State = EntityState.Detached;

            await logger.LogMessage($"Doslo je do greske prilikom cuvanja novog korisnika u bazu! {e.Message}", LogTypes.ERROR);
            throw new BadRequestException("Doslo je do greske, molimo vas probajte kasnije!");
        }

        await logger.LogMessage($"Korisnik [{novi.Id}]{username} se uspesno registrovao!", LogTypes.AUTH);

        return new UserInfoDto { Id = novi.Id, Username = username, Role = novi.Role, CreatedAt = novi.CreatedAt };
    }
}