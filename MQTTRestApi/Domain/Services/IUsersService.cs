using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IUsersService
{
    public List<User> GetUsers();
}