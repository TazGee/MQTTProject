using MQTTnet.Protocol;

namespace MQTTRestApi.Domain.DTO;

public class MqttMessageDto
{
    public int Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public MqttQualityOfServiceLevel QoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
    public bool Retain { get; set; }  = false;
    public DateTime RecievedAt { get; set; } = DateTime.MinValue;

    public MqttMessageDto(int  id, string topic, string payload, MqttQualityOfServiceLevel qos, bool retain, DateTime recievedAt)
    {
        Id = id;
        Topic = topic;
        Payload = payload;
        QoS = qos;
        Retain = retain;
        RecievedAt = recievedAt;
    }
}