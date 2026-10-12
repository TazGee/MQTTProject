using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

/// <summary>
/// Servis koji vrsi sve radnje backend-a
/// </summary>
public class MqttService : IMqttService
{
    private bool reconnecting = false;
    
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConfiguration config;
    
    private MqttClientFactory factory = new MqttClientFactory();
    private IMqttClient mqttClient;

    private int[] reconnectIntervali = [];
    int reconnectCounter = 0;
    
    private bool connected = false;
    
    private readonly IRedisService redis;
    
    private readonly HashSet<string> failedTopics = new ();
    
    public MqttService(IServiceScopeFactory scopeFactory, IRedisService redis, IConfiguration config)
        : this(scopeFactory, redis, config, new MqttClientFactory().CreateMqttClient())
    {
        
    }

    public MqttService(IServiceScopeFactory scopeFactory, IRedisService redis, IConfiguration config, IMqttClient mqttClient)
    {
        this.scopeFactory = scopeFactory;
        this.redis = redis;
        this.config = config;
        this.mqttClient = mqttClient;
        
        InitializeMqttClient();
    }
    
    private async Task LogAsync(string message,  LogTypes logType)
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerService>();
        await logger.LogMessage(message, logType);
    }
    
    void InitializeMqttClient()
    {
        try
        {
            mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine();
                Console.WriteLine("MQTT konekcija je prekinuta");

                connected = false;

                if (e.Exception != null)
                {
                    Console.WriteLine($"Razlog: {e.Exception.Message}");
                }

                if(!reconnecting) await ConnectAsync();
            };
            
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
        reconnectIntervali = config.GetSection("ReconnectTimerValues").Get<int[]>() ?? [];
        reconnectCounter = 0;
        
        reconnecting = true;
        do
        {
            if(connected)
            {
                reconnecting = false;
                break;
            }
            
            try
            {
                Console.WriteLine("Pokusavam povezivanje sa serverom...");

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(config["MqttBroker:Host"] ?? "localhost", 
                        int.Parse(config["MqttBroker:Port"] ?? "1883"))
                    .Build();
                
                await mqttClient.ConnectAsync(options);
                Console.WriteLine("Konekcija uspesno uspostavljena!");

                reconnecting = false;
                connected = true;
                
                await LogAsync($"Backend API se uspesno povezao na server.", LogTypes.INFO);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Povezivanje nije uspelo: {ex.Message}");
                Console.WriteLine($"Pokusaj ponovnog povezivanja za {reconnectIntervali[reconnectCounter]} sekundi...");
                await LogAsync($"Backend API je imao neuspesan pokusaj povezivanja!", LogTypes.ERROR);
                await Task.Delay(TimeSpan.FromSeconds(reconnectIntervali[reconnectCounter]));
                if(reconnectCounter < reconnectIntervali.Length - 1) reconnectCounter++;
            }
        } while (!mqttClient.IsConnected);
        try
        {
            await ResubscribeToAll();
            Console.WriteLine("Uspesan resubscribe na sve topic-e!");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Neuspesan pokusaj resubscribe-a: {e.Message}");
            await LogAsync($"Neuspesan pokusaj resubscribe-a na topic-e!", LogTypes.ERROR);
            throw;
        }
    }

    public async Task ResubscribeToAll()
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (Topic t in dbContext.Topics)
            {
                await SubscribeOnBrokerAsync(t.Name);
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
            
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (await dbContext.Topics.AnyAsync(s => s.Name == topic))
                {
                    Console.WriteLine($"Vec postoji subscribe na topic {topic}");
                    return false; 
                }
                
                MqttClientSubscribeResult result = await mqttClient.SubscribeAsync(subscribeOptions);
            
                foreach (var item in result.Items)
                {
                    var success = item.ResultCode is
                        MqttClientSubscribeResultCode.GrantedQoS0 or
                        MqttClientSubscribeResultCode.GrantedQoS1 or
                        MqttClientSubscribeResultCode.GrantedQoS2;

                    if (!success)
                    {
                        Console.WriteLine($"Subscribe nije uspeo!");
                        return false;
                    }
                }
                
                Topic top = new Topic(topic, String.Empty);
                
                dbContext.Topics.Add(top);
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

    private async Task<bool> SubscribeOnBrokerAsync(string topic)
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
            
            MqttClientSubscribeResult result = await mqttClient.SubscribeAsync(subscribeOptions);
            
            foreach (var item in result.Items)
            {
                var success = item.ResultCode is
                    MqttClientSubscribeResultCode.GrantedQoS0 or
                    MqttClientSubscribeResultCode.GrantedQoS1 or
                    MqttClientSubscribeResultCode.GrantedQoS2;

                if (!success)
                {
                    Console.WriteLine($"Subscribe nije uspeo!");
                    failedTopics.Add(topic);
                    return false;
                }
            }
            
            failedTopics.Remove(topic);
            Console.WriteLine($"Uspesan subscribe na topic {topic}!");
            return true;
        }
        catch (Exception e)
        {
            failedTopics.Add(topic);
            Console.WriteLine($"Greska pri subscribovanju: {e.Message}");
            return false;
        }
    }
    
    public async Task<bool> SubscribeUserAsync(SubscribeRequestDto request, int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var topic = await dbContext.Topics.FindAsync(request.TopicId);
        if (topic is null) return false;
        
        if (await dbContext.UserSubscriptions.AnyAsync(s => s.UserId == userId && s.TopicId == request.TopicId))
            return false;

        UserSubscription sub = new UserSubscription { UserId = userId, TopicId = request.TopicId };
        
        dbContext.UserSubscriptions.Add(sub);
        
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return false;
        }
        
        try
        {
            await redis.AddAsync(topic.Name, userId);
        }
        catch (Exception ex)
        {
            await LogAsync($"Redis nije uspeo da doda podatak u bazu, radi se rollback! {ex.Message}", LogTypes.ERROR);

            dbContext.UserSubscriptions.Remove(sub);
            await dbContext.SaveChangesAsync();

            return false;
        }
        
        return true;
    }

    public async Task<bool> UnsubscribeUserAsync(int topicId, int userId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
            var sub = await dbContext.UserSubscriptions.Include(s => s.Topic).FirstOrDefaultAsync(s => s.UserId == userId && s.TopicId == topicId);
            if (sub is null) return false;
        
            dbContext.UserSubscriptions.Remove(sub);
            await dbContext.SaveChangesAsync();
            
            try
            {
                await redis.RemoveAsync(sub.Topic.Name, userId);
            }
            catch (Exception ex)
            {
                await LogAsync($"Redis nije uspeo da ukloni podatak, radi se rollback! {ex.Message}", LogTypes.ERROR);

                dbContext.UserSubscriptions.Add(sub);
                await dbContext.SaveChangesAsync();
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri unsubscribovanju: {e.Message}");
            return false;
        }
    }
    
    public async Task<List<MqttMessageDto>> GetMessages(int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var filters = await dbContext.UserSubscriptions
            .Where(s => s.UserId == userId)
            .Select(s => s.Topic.Name)
            .ToListAsync();

        var messages = await dbContext.Messages
            .OrderByDescending(m => m.Id)
            .Select(m => new MqttMessageDto(m.Id, m.Topic, m.Payload, m.QoS, m.Retain, m.RecievedAt))
            .ToListAsync();

        return messages
            .Where(m => filters.Any(f => TopicMatches(f, m.Topic)))
            .ToList();
    }
    
    private static bool TopicMatches(string filter, string topic)
    {
        var f = filter.Split('/');
        var t = topic.Split('/');

        for (var i = 0; i < f.Length; i++)
        {
            if (f[i] == "#") return true;
            if (i >= t.Length) return false;
            if (f[i] != "+" && f[i] != t[i]) return false;
        }

        return f.Length == t.Length;
    }
    
    public async Task<List<TopicListDto>> MyTopics(int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.UserSubscriptions.Where(s => s.UserId == userId)
            .Select(s => new TopicListDto(s.TopicId, s.Topic.Name)).ToListAsync();
    }
    
    public List<Topic> GetFailedTopics()
    {
        return failedTopics.Select(t => new Topic(t, String.Empty)).ToList();
    }

    public async Task<List<string>> ResubscribeToFailed()
    {
        List<string> results = new List<string>();

        foreach (var name in failedTopics.ToList())
        {
            results.Add(await SubscribeOnBrokerAsync(name)
                ? $"Uspesan resubscribe na {name}!"
                : $"Neuspesan pokusaj resubscribe na {name}");
        }

        return results;
    }

    public async Task<bool> ForceReconnect()
    {
        if (connected) return false;
        
        try
        {
            Console.WriteLine("Pokusavam povezivanje sa serverom...");

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(config["MqttBroker:Host"] ?? "localhost", 
                    int.Parse(config["MqttBroker:Port"] ?? "1883"))
                .Build();
                
            await mqttClient.ConnectAsync(options);
            Console.WriteLine("Konekcija uspesno uspostavljena!");

            reconnecting = false;
            connected = true;

            await LogAsync($"Backend API se uspesno povezao na server.", LogTypes.INFO);    
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Povezivanje nije uspelo: {ex.Message}");
            return false;
        }

        try
        {
            await ResubscribeToAll();
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}