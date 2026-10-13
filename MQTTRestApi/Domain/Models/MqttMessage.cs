using MQTTnet.Protocol;
using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Models;

/// <summary>
/// Predstavlja poruku koja se salje.
/// </summary>
public class MqttMessage
{
    public int Id { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public MqttQualityOfServiceLevel QoS { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
    public bool Retain { get; set; } = false;
    public DateTime RecievedAt { get; set; } = DateTime.MinValue;
    public ValueAlarmTypes AlarmType { get; set; } = ValueAlarmTypes.NONE;

    public MqttMessage() { }
    
    public MqttMessage(string Topic, string Payload, MqttQualityOfServiceLevel QoS, bool Retain, DateTime RecievedAt, ValueAlarmTypes AlarmType)
    {
        this.Topic = Topic;
        this.Payload = Payload;
        this.QoS = QoS;
        this.Retain = Retain;
        this.RecievedAt = RecievedAt;
        this.AlarmType = AlarmType;
    }
    /// <summary>
    /// Pretvara objekat u string
    /// </summary>
    /// <returns>String sa podacima o objektu.</returns>
    public override string ToString()
    {
        return $"\n[{RecievedAt:HH:mm:ss}] | {Topic} | {Payload} | {QoS} | {Retain}";
    }
}