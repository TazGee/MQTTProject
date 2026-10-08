using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class RegisterDto
{
    public string Username { get; set; }
    public string Password { get; set; }
    public UserTypes UserType { get; set; }

    public RegisterDto(string username, string password, UserTypes userType)
    {
        Username = username;
        Password = password;
        UserType = userType;
    }
}