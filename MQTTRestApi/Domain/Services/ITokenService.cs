namespace MQTTRestApi.Domain.Services;

public interface ITokenService
{
    string CreateToken(string userId, string username, string role);
}