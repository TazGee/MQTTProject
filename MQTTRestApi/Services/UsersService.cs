using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
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
    
    public List<UserInfoDto> GetUsers()
    {
        var users = dbContext.Users.ToList();

        var userDtos = users.Select(u => new UserInfoDto
        {
            Id = u.Id,
            Username = u.Username,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList();
        
        return userDtos;
    }
}