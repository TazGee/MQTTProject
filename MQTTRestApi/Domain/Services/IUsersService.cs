using MQTTRestApi.Domain.DTO;

namespace MQTTRestApi.Domain.Services;

public interface IUsersService
{
    public List<UserInfoDto> GetUsers();
}