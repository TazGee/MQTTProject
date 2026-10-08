using MQTTRestApi.Data;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext dbContext;
    
    public AuthService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public (User, bool) Login(string username, string password)
    {
        foreach (var user in dbContext.Users)
        {
            if (String.Equals(user.Username, username) && String.Equals(user.Password, password))
            {
                return (user, true);
            }
        }
        return (new User(), false);
    }

    public (User, bool) Register(string username, string password, UserTypes TipKorisnika)
    {
        foreach (var user in dbContext.Users)
        {
            if (String.Equals(user.Username, username) && String.Equals(user.Password, password))
            {
                return (new User(), false);
            }
        }
        
        User novi = new User(username, password, TipKorisnika);

        try
        {
            dbContext.Users.Add(novi);
            dbContext.SaveChanges();
            return (novi, true);
        }
        catch (Exception e)
        {
            Console.WriteLine("Greska pri dodavanju korisnika u bazu: " + e.Message);
            return (new User(), false);
        }
    }
}