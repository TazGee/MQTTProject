using System.Text.RegularExpressions;
using MQTTnet;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

/// <summary>
/// Servis koji vrsi sve radnje backend-a
/// </summary>
public class MqttService : IMqttService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration config;
    
    private MqttClientFactory factory = new MqttClientFactory();
    private IMqttClient mqttClient;
    
    private bool connected = false;
    
    private readonly RedisService redis;
    
    public MqttService(IServiceScopeFactory scopeFactory, RedisService redis, IConfiguration config)
        : this(scopeFactory, redis, config, new MqttClientFactory().CreateMqttClient())
    {
        
    }

    public MqttService(IServiceScopeFactory scopeFactory, RedisService redis, IConfiguration config, IMqttClient mqttClient)
    {
        this.scopeFactory = scopeFactory;
        this.redis = redis;
        this.config = config;
        this.mqttClient = mqttClient;

        InitializeMqttClient();
    }
    
    void InitializeMqttClient()
    {
        try
        {
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                MqttMessage poruka = new MqttMessage(e.ApplicationMessage.Topic,
                                             e.ApplicationMessage.ConvertPayloadToString(),
                                             e.ApplicationMessage.QualityOfServiceLevel,
                                             e.ApplicationMessage.Retain,
                                             DateTime.Now);
                
                Console.WriteLine(poruka.ToString());
                try
                {
                    using (var scope = scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        dbContext.Messages.Add(poruka);
                    
                        var dbTask = Task.Run(async () =>
                        {
                            await dbContext.SaveChangesAsync();
                        });

                        var redisTask = Task.Run(async () =>
                        {
                            await redis.SaveMessageAsync(poruka);
                        });

                        await Task.WhenAll(dbTask, redisTask);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska pri cuvanju u bazu i/ili redis: {ex.Message}");
                    throw;
                }
            };
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom inicijalizacije klijenta: {e.Message}");
        }
    }

    public async Task ConnectAsync()
    {
        do
        {
            try
            {
                Console.WriteLine("Pokusavam povezivanje sa serverom...");

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(config["MqttBroker:Host"] ?? "localhost", 
                        int.Parse(config["MqttBroker:Port"] ?? "1883"))
                    .Build();
                
                await mqttClient.ConnectAsync(options);
                Console.WriteLine("Konekcija uspesno uspostavljena!");

                await ResubscribeToAll();
                Console.WriteLine("Uspesno resubscribed na sve topice!");
                
                connected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Povezivanje nije uspelo: {ex.Message}");
                Console.WriteLine($"Pokusaj ponovnog povezivanja za {config["Settings:ReconnectTimer"]} sekundi...");
                await Task.Delay(TimeSpan.FromSeconds(int.Parse(config["Settings:ReconnectTimer"] ?? "3")));
            }
        } while (!mqttClient.IsConnected);
            
        Console.WriteLine("\nPovezivanje uspesno!");
    }

    public async Task ResubscribeToAll()
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (Subscription s in dbContext.Subscriptions)
            {
                try
                {
                    await SubscribeAsync(s.Topic);
                    Console.WriteLine($"{s.Topic} ponovo subscribed");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Greska pri re-subscribeu: {e.Message}");
                }
            }
        }
    }
    
    public async Task<bool> SubscribeAsync(string topic)
    {
        try
        {
            if (!Regex.IsMatch(topic, config["SubscribeRegex"]))
            {
                Console.WriteLine($"Vrednost topica nije validna!");
                return false;
            }
            
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();
            
            await mqttClient.SubscribeAsync(subscribeOptions);

            if (!connected) return true;
            
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                Subscription sub = new Subscription(topic, DateTime.Now);
                
                dbContext.Subscriptions.Add(sub);
                await dbContext.SaveChangesAsync();
            }
            
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
            if (!Regex.IsMatch(topic, config["PublishRegex"]))
            {
                Console.WriteLine($"Vrednost topica nije validna!");
                return false;
            }
            
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
    
    public List<MqttMessage> GetMessages()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        return dbContext.Messages.ToList();
    }
}