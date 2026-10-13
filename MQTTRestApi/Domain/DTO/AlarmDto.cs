using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class AlarmDto
{
    public int Id { get; set; }
    public int TopicId { get; set; } = 0;
    public AlarmTypes AlarmType { get; set; } = AlarmTypes.Both;
    public double LowerLimit { get; set; } = 0;
    public double UpperLimit { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public AlarmDto(int id, int topicId, AlarmTypes alarmType, double lowerLimit, double upperLimit, DateTime createdAt)
    {
        Id = id;
        TopicId = topicId;
        AlarmType = alarmType;
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
        CreatedAt = createdAt;
    }
}