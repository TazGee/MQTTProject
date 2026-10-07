using MQTTnet.Protocol;

namespace MQTTGitProject.Models;

public class Subscription
{
    public string Topic { get; set; }
    public MqttQualityOfServiceLevel QoS { get; set; }
    
    public Subscription(string Topic, MqttQualityOfServiceLevel QoS)
    {
        this.Topic = Topic;
        this.QoS = QoS;
    }
}