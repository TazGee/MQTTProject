using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
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
    
    public (UserInfoDto, bool) Login(string username, string password)
    {
        foreach (var user in dbContext.Users)
        {
            if (String.Equals(user.Username, username) && String.Equals(user.Password, password))
            {
                logger.LogMessage($"Korisnik {username} se uspesno ulogovao na nalog.", LogTypes.AUTH);
                UserInfoDto u = new UserInfoDto() { Id = user.Id, Username = username, Role = user.Role, CreatedAt = user.CreatedAt };
                return (u, true);
            }
        }

        logger.LogMessage($"Neuspesan pokusaj logovanja ({username})!", LogTypes.AUTH);
        return (new UserInfoDto(), false);
    }

    public (UserInfoDto, bool) Register(string username, string password, UserTypes TipKorisnika)
    {
        foreach (var user in dbContext.Users)
        {
            if (String.Equals(user.Username, username))
            {
                logger.LogMessage($"Neuspesan pokusaj registracije ({username} vec postoji)!", LogTypes.AUTH);
                return (new UserInfoDto(), false);
            }
        }
        
        User novi = new User(username, password, TipKorisnika);

        try
        {
            dbContext.Users.Add(novi);
            dbContext.SaveChanges();
            logger.LogMessage($"Korisnik {username} se uspesno registrova.", LogTypes.AUTH);
            UserInfoDto u = new UserInfoDto() { Id = novi.Id, Username = username, Role = novi.Role, CreatedAt = novi.CreatedAt };
            return (u, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("Greska pri dodavanju korisnika u bazu: " + e.Message);
            logger.LogMessage($"Neuspesan pokusaj registracije ({username} - EXCEPTION)!", LogTypes.ERROR);
            return (new UserInfoDto(), false);
        }
    }
}