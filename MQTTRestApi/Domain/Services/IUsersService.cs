using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Services;

public interface IUsersService
{
    public List<UserInfoDto> GetUsers();
    public Task ChangePassword(int userId, ChangePasswordDto dto);
    public Task ChangeRole(int UserId, UserTypes role);
}