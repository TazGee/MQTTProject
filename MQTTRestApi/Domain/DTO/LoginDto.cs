namespace MQTTRestApi.Domain.DTO;

public class LoginDto
{
    public string Username { get; set; }
    public string Password { get; set; }

    public LoginDto(string username, string password)
    {
        Username = username;
        Password = password;
    }
}