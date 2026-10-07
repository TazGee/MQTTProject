using MQTTnet;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Services;

public class MqttService
{
    private List<Message> messages = new List<Message>();
    
    private MqttClientFactory factory = new MqttClientFactory();
    private IMqttClient mqttClient;
    
    private bool connected = false;

    public MqttService()
    {
        InitializeMqttClient();
    }
    
    void InitializeMqttClient()
    {
        try
        {
            mqttClient = factory.CreateMqttClient();
            
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Message poruka = new Message(e.ApplicationMessage.Topic,
                                             e.ApplicationMessage.ConvertPayloadToString(),
                                             e.ApplicationMessage.QualityOfServiceLevel,
                                             e.ApplicationMessage.Retain);
                
                messages.Add(poruka);
                Console.WriteLine(poruka.ToString());
                
                return Task.CompletedTask;
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom inicijalizacije klijenta: {e.Message}");
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();
            
            await mqttClient.ConnectAsync(options);

            connected = true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri povezivanju: {e.Message}");
        }

        return connected;
    }
    
    public async Task<bool> SubscribeAsync(string topic)
    {
        try
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();
            
            await mqttClient.SubscribeAsync(subscribeOptions);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri subscribovanju: {e.Message}");
            return false;
        }
    }
    
    public async Task<bool> PublishAsync(string topic, string payload, MQTTnet.Protocol.MqttQualityOfServiceLevel qos)
    {
        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(qos)
                .Build();

            await mqttClient.PublishAsync(message);

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri subscribovanju: {e.Message}");
            return false;
        }
    }
    
    public List<Message> GetMessages()
    {
        return messages;
    }
}