using MQTTnet.Protocol;
using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.DTO;

public class MqttMessageDto
{
    public int Id { get; set; }
    public string Topic { get; set; } 
    public string Payload { get; set; }
    public MqttQualityOfServiceLevel QoS { get; set; } 
    public bool Retain { get; set; }
    public DateTime RecievedAt { get; set; }
    public ValueAlarmTypes AlarmType { get; set; }

    public MqttMessageDto(int  id, string topic, string payload, MqttQualityOfServiceLevel qos, bool retain, DateTime recievedAt, ValueAlarmTypes alarmType)
    {
        Id = id;
        Topic = topic;
        Payload = payload;
        QoS = qos;
        Retain = retain;
        RecievedAt = recievedAt;
        AlarmType = alarmType;
    }
}