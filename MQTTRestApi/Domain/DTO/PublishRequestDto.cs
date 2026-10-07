using MQTTnet.Protocol;

namespace MQTTRestApi.Domain.DTO;

public class PublishRequestDto
{
    public string Topic { get; set; }
    public string Payload { get; set; }
    public MqttQualityOfServiceLevel QoS { get; set; }
}