using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IAuthService
{
    (User, bool) Login(string username, string password);
    (User, bool) Register(string username, string password, UserTypes TipKorisnika);
}