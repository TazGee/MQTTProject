using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface IAlarmService
{
    Task CreateAlarm(CreateAlarmDto createAlarmDto);
    Task DeleteAlarm(int id);
    Task<List<AlarmDto>> GetAllAlarms();
    Task<List<MqttMessageDto>> GetAllAlarmedMessages();
}