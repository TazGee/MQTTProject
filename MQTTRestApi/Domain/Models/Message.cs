using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTRestApi.Domain.Models;

public class Message
{
    string Topic  { get; set; } = string.Empty;
    string Payload  { get; set; } = string.Empty;
    MqttQualityOfServiceLevel QoS { get; set; } =  MqttQualityOfServiceLevel.AtMostOnce;
    bool Retain  { get; set; } = false;

    public Message() {}
    public Message(string topic, string payload, MqttQualityOfServiceLevel qoS, bool retain)
    {
        Topic = topic;
        Payload = payload;
        QoS = qoS;
        Retain = retain;
    }
    
    public override string ToString()
    {
        return $"\n[{DateTime.Now:HH:mm:sss}] " +
               $"| {Topic} | " +
               $"{Payload} | " +
               $"{QoS} | " +
               $"{(Retain ? "Retain" : "No Retain")}";
    }
}