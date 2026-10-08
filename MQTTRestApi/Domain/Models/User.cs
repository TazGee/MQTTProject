using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserTypes Role { get; set; } = UserTypes.User;
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    
    public User () {}
    public User(string username, string password, UserTypes role)
    {
        Username = username;
        Password = password;
        Role = role;
        CreatedAt =  DateTime.Now;
    }
}