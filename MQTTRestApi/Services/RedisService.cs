using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTRestApi.Data;
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
    private readonly IDatabase redisdb;

    public RedisService()
    {
        
    }
    public RedisService(IConnectionMultiplexer redis, IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
        this.redis = redis;
        this.config = config;
        redisdb = redis.GetDatabase();
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
    
    public async Task AddAsync(string topic, int userId)
    {
        await redisdb.SetAddAsync($"topic:{topic}:users", userId);
        await redisdb.SetAddAsync($"user:{userId}:topics", topic);
    }

    public async Task RemoveAsync(string topic, int userId)
    {
        await redisdb.SetRemoveAsync($"topic:{topic}:users", userId);
        await redisdb.SetRemoveAsync($"user:{userId}:topics", topic);
    }

    public async Task<bool> IsSubscribedAsync(string topic, int userId)
    {
        return await redisdb.SetContainsAsync($"topic:{topic}:users", userId);
    }

    public async Task<IReadOnlyList<string>> GetUserTopicsAsync(int userId)
    {
        return (await redisdb.SetMembersAsync($"user:{userId}:topics")).Select(v => v.ToString()).ToList();
    }
    
    public async Task LoadAllAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var subs = await db.UserSubscriptions.Include(s => s.Topic).ToListAsync();
        
        foreach (var s in subs) await AddAsync(s.Topic.Name, s.UserId);
    }
}