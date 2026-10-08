using MQTTRestApi.Data;
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
    
    public List<User> GetUsers()
    {
        return dbContext.Users.ToList();
    }
}