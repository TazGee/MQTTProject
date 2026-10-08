using MQTTnet.Protocol;

namespace MQTTRestApi.Domain.DTO;

/// <summary>
/// Data transfer object za zahtev za slanje poruke
/// </summary>
public class PublishRequestDto
{
    public string Topic { get; set; }
    public string Payload { get; set; }
    public MqttQualityOfServiceLevel QoS { get; set; }
}