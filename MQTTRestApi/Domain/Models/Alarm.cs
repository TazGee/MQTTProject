using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Models;

public class Alarm
{
    public int Id { get; set; }
    public int TopicId { get; set; } = 0;
    public AlarmTypes AlarmType { get; set; } = AlarmTypes.Both;
    public int LowerLimit { get; set; } = 0;
    public int UpperLimit { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public Alarm() { }
    
    public Alarm(int topicId, AlarmTypes alarmType,  int lowerLimit, int upperLimit)
    {
        TopicId = topicId;
        AlarmType = alarmType;
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
        CreatedAt = DateTime.Now;
    }
}