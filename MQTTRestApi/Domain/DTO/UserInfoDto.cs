using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class UserInfoDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserTypes Role { get; set; } = UserTypes.User;
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    
}