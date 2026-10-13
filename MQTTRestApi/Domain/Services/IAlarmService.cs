using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Services;

public interface IAlarmService
{
    Task CreateAlarm(int topicId, AlarmTypes alarmType,  int lowerLimit, int upperLimit);
    Task DeleteAlarm(int id);
}