using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class RegisterDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public UserTypes UserType { get; set; }

    public RegisterDto(string username, string password)
    {
        Username = username;
        Password = password;
    }
}