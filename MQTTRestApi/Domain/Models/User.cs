using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Models;

public class User
{
    public long Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public UserTypes Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    
    public User () {}
    public User(string username, string password, UserTypes role)
    {
        Id = DateTime.Now.Ticks;
        Username = username;
        Password = password;
        Role = role;
        CreatedAt =  DateTime.Now;
    }
}