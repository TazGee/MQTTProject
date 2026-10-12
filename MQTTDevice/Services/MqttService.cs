using MQTTDevice.Domain.Services;
using MQTTGitProject.Config;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTDevice.Services;

public class MqttService : IMqttService
{
    private bool connected = false;
    private bool reconnecting = false;
    private IMqttClient client;

    public async Task InitializeService(Config config)
    {
        try
        {
            var factory = new MqttClientFactory();
            client = factory.CreateMqttClient();
            
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(config.ServerIP, config.ServerPort)
                .Build();
            
            await TryToConnect(options, config);
            
            client.DisconnectedAsync += async e =>
            {
                Console.WriteLine();
                Console.WriteLine("MQTT konekcija je prekinuta");

                connected = false;
                
                if (e.Exception != null)
                {
                    Console.WriteLine($"Razlog: {e.Exception.Message}");
                }

                if(!reconnecting) await TryToConnect(options, config);
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom inicijalizacije servisa: {e.Message}");
            throw;
        }
    }
    
    async Task TryToConnect(MqttClientOptions options, Config config)
    {
        reconnecting = true;
        do
        {
            try
            {
                Console.WriteLine("Pokusavam povezivanje sa serverom...");

                await client.ConnectAsync(options);
                Console.WriteLine("Konekcija uspesno uspostavljena!");

                reconnecting = false;
                connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Povezivanje nije uspelo: {ex.Message}");
                Console.WriteLine($"Pokusaj ponovnog povezivanja za {config.ReconnectTimer} sekundi...");
                await Task.Delay(TimeSpan.FromSeconds(config.ReconnectTimer));
            }
        } while (!client.IsConnected);
            
        Console.WriteLine("\nPovezivanje uspesno!");
    }
    
    public async Task PublishMessageAsync(string topic, string payload)
    {
        if(!connected) return;
        
        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            await client.PublishAsync(message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri slanju poruke: {e.Message}");
        }
            
    }
}