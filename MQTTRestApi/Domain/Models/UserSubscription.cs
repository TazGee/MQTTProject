namespace MQTTRestApi.Domain.Models;

public class UserSubscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TopicId { get; set; }
    public DateTime CreatedAt { get; set; } =  DateTime.Now;

    public User User { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
    
    public UserSubscription() { }

    public UserSubscription(User user, Topic topic)
    {
        User = user;
        Topic = topic;
    }
}