using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class CreateAlarmDto
{
    public int topicID { get; set; }
    public AlarmTypes AlarmType { get; set; }
    public double lowerLimit { get; set; }
    public double upperLimit { get; set; }

    public CreateAlarmDto(int topicID, AlarmTypes alarmType, double lowerLimit, double upperLimit)
    {
        this.topicID = topicID;
        this.AlarmType = alarmType;
        this.lowerLimit = lowerLimit;
        this.upperLimit = upperLimit;
    }
}