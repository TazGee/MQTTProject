using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IAlarmService
{
    Task CreateAlarm(int topicId, AlarmTypes alarmType,  int lowerLimit, int upperLimit);
    Task DeleteAlarm(int id);
    Task<List<Alarm>> GetAllAlarms();
    public Task<List<MqttMessageDto>> GetAllAlarmedMessages();
}