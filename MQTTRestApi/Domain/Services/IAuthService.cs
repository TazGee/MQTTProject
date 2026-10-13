using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Services;

public interface IAuthService
{
    Task<UserInfoDto> Login(string username, string password);
    Task<UserInfoDto> Register(string username, string password, UserTypes TipKorisnika);
}