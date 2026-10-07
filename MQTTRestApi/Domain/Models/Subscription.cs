namespace MQTTRestApi.Domain.Models;

/// <summary>
/// Predstavlja pretplatu na neki topic
/// </summary>
public class Subscription
{
    public long Id { get; set; } = 0;
    public string Topic { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.MinValue;

    public Subscription() { }
    
    public Subscription(string Topic, DateTime CreatedAt)
    {
        Id = DateTime.Now.Millisecond;
        this.Topic = Topic;
        this.CreatedAt = CreatedAt;
    }
}