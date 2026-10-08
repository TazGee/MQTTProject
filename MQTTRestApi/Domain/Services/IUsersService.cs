using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IUsersService
{
    public List<UserInfoDto> GetUsers();
}