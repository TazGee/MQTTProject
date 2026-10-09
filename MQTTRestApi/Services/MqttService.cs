using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
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
    
    List<Topic> failedTopics = new List<Topic>();
    
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
        }
    }

    public async Task ResubscribeToAll()
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (Topic t in dbContext.Topics)
            {
                if(await SubscribeAsync(t.Name)) Console.WriteLine($"{t.Name} ponovo subscribed");
                else failedTopics.Add(t);
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

            if (connected) return true;
            
            using (var scope = scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (await dbContext.Topics.AnyAsync(s => s.Name == topic))
                {
                    Console.WriteLine($"Vec postoji subscribe na topic {topic}");
                    return false; 
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
    
    public async Task SubscribeUserAsync(SubscribeRequestDto request, int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var topic = await dbContext.Topics.FindAsync(request.TopicId)
            ?? throw new NotFoundException($"Topic {request.TopicId} ne postoji.");
        
        if (await dbContext.UserSubscriptions.AnyAsync(s => s.UserId == userId && s.TopicId == request.TopicId))
            throw new ConflictException($"Vec si prijavljen na topic {request.TopicId}!");
        
        dbContext.UserSubscriptions.Add(new UserSubscription { UserId = userId, TopicId = request.TopicId });
        await dbContext.SaveChangesAsync();
        await redis.AddAsync(topic.Name, userId);
    }

    public async Task UnsubscribeUserAsync(int topicId, int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
        var sub = await dbContext.UserSubscriptions.Include(s => s.Topic).FirstOrDefaultAsync(s => s.UserId == userId && s.TopicId == topicId);
        if (sub == null) throw new NotFoundException($"Topic {topicId} ne postoji.");
    
        dbContext.UserSubscriptions.Remove(sub);
        await dbContext.SaveChangesAsync();
        await redis.RemoveAsync(sub.Topic.Name, userId);
    }
    
    public async Task<List<MqttMessageDto>> GetMessages(int userId)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
        var topics = dbContext.UserSubscriptions
            .Where(s => s.UserId == userId)
            .Select(s => s.Topic.Name);
    
        return await dbContext.Messages
            .Where(m => topics.Contains(m.Topic))
            .OrderByDescending(m => m.Id)
            .Select(m => new MqttMessageDto(m.Id, m.Topic, m.Payload, m.QoS, m.Retain, m.RecievedAt))
            .ToListAsync();
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
        return failedTopics;
    }

    public async Task<List<string>> ResubscribeToFailed()
    {
        List<string> results = new List<string>();
        
        foreach (Topic t in failedTopics)
        {
            if (await SubscribeAsync(t.Name))results.Add($"Uspesan resubscribe na {t.Name}!");
            else results.Add($"Neuspesan pokusaj resubscribe na {t.Name}");
        }
        
        return results;
    }

    public async Task ForceReconnect()
    {
        if (connected) throw new BadRequestException($"Server je vec povezan!");
        
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
            throw new BadRequestException($"Server je vec povezan!");
        }
    }
}