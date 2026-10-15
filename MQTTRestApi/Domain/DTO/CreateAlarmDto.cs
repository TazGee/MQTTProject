using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class CreateAlarmDto
{
    public int TopicId { get; set; }
    public AlarmTypes AlarmType { get; set; }
    public double LowerLimit { get; set; }
    public double UpperLimit { get; set; }

    public CreateAlarmDto(int topicId, AlarmTypes alarmType, double lowerLimit, double upperLimit)
    {
        TopicId = topicId;
        AlarmType = alarmType;
        LowerLimit = lowerLimit;
        UpperLimit = upperLimit;
    }
}