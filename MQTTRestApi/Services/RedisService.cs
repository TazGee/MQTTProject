using System.Text.Json;
using System.Text.RegularExpressions;
using MQTTnet;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using StackExchange.Redis;

namespace MQTTRestApi.Services;

/// <summary>
/// Servis koji komunicira sa redis serverom
/// </summary>
public class RedisService : IRedisService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IConnectionMultiplexer redis;
    private readonly IConfiguration config;

    public RedisService()
    {
        
    }
    public RedisService(IConnectionMultiplexer redis, IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
        this.redis = redis;
        this.config = config;
    }

    private async Task LogAsync(string message,  LogTypes logType)
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerService>();
        await logger.LogMessage(message, logType);
    }
    
    public async Task SaveMessageAsync(MqttMessage message)
    {
        try
        {
            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(message);

            var key = $"{message.Id}:{message.Topic}";

            Console.WriteLine($"Redis key: {key}");
            Console.WriteLine($"Redis JSON: {json}");

            await db.StringSetAsync(
                key,
                json,
                TimeSpan.FromHours(1)
            );

            Console.WriteLine("Poruka sacuvana u Redis!");

            await LogAsync($"Poruka za topic {message.Topic} sacuvana u redisu!", LogTypes.REDIS);
            
            await IncrementTopicCountAsync(message.Topic);
        }
        catch (Exception e)
        {
            Console.WriteLine($"GRESKA REDIS: {e}");
        }
    }

    
    public async Task IncrementTopicCountAsync(string topic)
    {
        try
        {
            if (!Regex.IsMatch(topic, config["PublishRegex"]))
            {
                Console.WriteLine($"Vrednost topica nije validna!");
            }
        
            var db = redis.GetDatabase();
            var key = $"topic-count:{topic}";

            await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
            await LogAsync($"Inkrementiran brojac za topic {topic} u redisu!", LogTypes.REDIS);
        }
        catch (Exception e)
        {
            await LogAsync($"Greska prilikom inkrementiranja brojaca za topic {topic} u redisu!", LogTypes.REDIS);
            throw;
        }
    }
    
    public async Task<Dictionary<string, int>> GetAllTopicsCountAsync()
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());
        
        var result = new Dictionary<string, int>();

        await foreach (var key in server.KeysAsync(pattern: "topic-count:*"))
        {
            var topic = key.ToString()["topic-count:".Length..];

            var value = await db.StringGetAsync(key);

            if (value.HasValue)
            {
                result[topic] = (int)value;
            }
        }

        return result;
    }
    
    public async Task<int> GetTopicCountAsync(string topic)
    {
        try
        {
            if (!Regex.IsMatch(topic, config["PublishRegex"]))
            {
                Console.WriteLine($"Vrednost topica nije validna!");
            }
        
            var db = redis.GetDatabase();
            var key = $"topic-count:{topic}";
        
            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
            {
                return 0;
            }

            return (int)value;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska pri dobijanju topic count-a: {e.Message}");
            return 0;
        }
    }
}