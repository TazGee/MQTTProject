using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IAuthService
{
    (UserInfoDto, bool) Login(string username, string password);
    (UserInfoDto, bool) Register(string username, string password, UserTypes TipKorisnika);
}