using MQTTDevice.Domain.Services;
using MQTTnet;

namespace MQTTDevice.Services;

public class MqttService : IMqttService
{
    private bool connected = false;
    private bool reconnecting = false;
    private IMqttClient client;

    public float VrednostSimulatora { get; set; } = 0f;
    public string DeviceValueTopic { get; set; } = String.Empty;

    public async Task InitializeService(Config.Config config)
    {
        try
        {
            var factory = new MqttClientFactory();
            client = factory.CreateMqttClient();
            
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(config.ServerIp, config.ServerPort)
                .Build();
            
            await TryToConnect(options, config);
            
            client.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    VrednostSimulatora = float.Parse(e.ApplicationMessage.ConvertPayloadToString());
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska pri ucitavanju pristigle vrednosti: {ex.Message}");
                    throw;
                }
            };
            
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
    
    async Task TryToConnect(MqttClientOptions options, Config.Config config)
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

        try
        {
            await SubscribeAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri pretplati: {e.Message}");
        }
        
        Console.WriteLine("\nPovezivanje uspesno!");
    }
    
    public async Task PublishMessageAsync(string topic)
    {
        if(!connected)
        {
            Console.WriteLine($"Simulator nije povezan na broker!");
            return;
        }
        
        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(VrednostSimulatora.ToString())
                .Build();

            await client.PublishAsync(message);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri slanju poruke: {e.Message}");
        }
    }
    
    public async Task SubscribeAsync()
    {
        try
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(DeviceValueTopic)
                .Build();
        
            MqttClientSubscribeResult result = await client.SubscribeAsync(subscribeOptions);
        
            foreach (var item in result.Items)
            {
                var success = item.ResultCode is
                    MqttClientSubscribeResultCode.GrantedQoS0 or
                    MqttClientSubscribeResultCode.GrantedQoS1 or
                    MqttClientSubscribeResultCode.GrantedQoS2;

                if (!success)
                {
                    throw new Exception("Nije moguce pretplatiti se na topic. Probajte da uneseti drugi topic ili probajte ponovo.");
                }
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Nije moguce pretplatiti se na topic. Probajte da uneseti drugi topic ili probajte ponovo. {e.Message}");
        }
    }
}