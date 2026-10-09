namespace MQTTRestApi.Domain.Models;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public Topic() { }
    
    public Topic(string name, string description)
    {
        Name = name;
        Description = description;
    }
}