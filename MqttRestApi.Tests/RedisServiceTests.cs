using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MQTTnet.Protocol;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;
using StackExchange.Redis;

namespace MqttRestApi.Tests;

public class RedisServiceTests
{
    private static (RedisService svc, Mock<IDatabase> db, Mock<ILoggerService> logger) Create(Action<Mock<IConnectionMultiplexer>, Mock<IDatabase>>? configureMux = null)
    {
        var db = new Mock<IDatabase>();
        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);
        configureMux?.Invoke(mux, db);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublishRegex"] = @"^[^/#+\s]+(?:/[^/#+\s]+)*$"
            })
            .Build();

        var logger = new Mock<ILoggerService>();
        var services = new ServiceCollection();
        services.AddScoped<ILoggerService>(_ => logger.Object);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return (new RedisService(mux.Object, config, scopeFactory), db, logger);
    }

    private static MqttMessage Msg(string topic, string payload = "22") =>
        new(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, DateTime.Now, ValueAlarmTypes.NONE);

    private static List<IInvocation> Calls(Mock<IDatabase> db, string methodName) =>
        db.Invocations.Where(i => i.Method.Name == methodName).ToList();

    [Fact]
    public async Task AddAsync_AddsUserToTopicSetAndTopicToUserSet()
    {
        var (svc, db, _) = Create();

        await svc.AddAsync("kuca/test", 7);

        db.Verify(d => d.SetAddAsync(
            It.Is<RedisKey>(k => k == "topic:kuca/test:users"),
            It.Is<RedisValue>(v => v == 7),
            It.IsAny<CommandFlags>()), Times.Once);
        db.Verify(d => d.SetAddAsync(
            It.Is<RedisKey>(k => k == "user:7:topics"),
            It.Is<RedisValue>(v => v == "kuca/test"),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_FirstRemovalFails_ThrowsBadRequest()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(false);

        await Assert.ThrowsAsync<BadRequestException>(() => svc.RemoveAsync("kuca/test", 7));
    }

    [Fact]
    public async Task RemoveAsync_BothRemovalsSucceed_DoesNotThrow()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(true);

        await svc.RemoveAsync("kuca/test", 7);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsSubscribedAsync_ReturnsRedisAnswer(bool contains)
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.SetContainsAsync(
                It.Is<RedisKey>(k => k == "topic:kuca/test:users"),
                It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(contains);

        Assert.Equal(contains, await svc.IsSubscribedAsync("kuca/test", 7));
    }

    [Fact]
    public async Task GetUserTopicsAsync_ReturnsTopicNames()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.SetMembersAsync(It.Is<RedisKey>(k => k == "user:7:topics"), It.IsAny<CommandFlags>()))
          .ReturnsAsync(new RedisValue[] { "kuca/a", "kuca/b" });

        var result = await svc.GetUserTopicsAsync(7);

        Assert.Equal(new[] { "kuca/a", "kuca/b" }, result);
    }

    [Fact]
    public async Task GetTopicCountAsync_KeyExists_ReturnsCount()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k == "topic-count:kuca/test"), It.IsAny<CommandFlags>()))
          .ReturnsAsync((RedisValue)5);

        Assert.Equal(5, await svc.GetTopicCountAsync("kuca/test"));
    }

    [Fact]
    public async Task GetTopicCountAsync_KeyMissing_ReturnsZero()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(RedisValue.Null);

        Assert.Equal(0, await svc.GetTopicCountAsync("kuca/test"));
    }

    [Fact]
    public async Task GetTopicCountAsync_RedisThrows_ReturnsZero()
    {
        var (svc, db, _) = Create();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
          .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        Assert.Equal(0, await svc.GetTopicCountAsync("kuca/test"));
    }

    [Fact]
    public async Task SaveMessageAsync_StoresSerializedMessageWithOneHourTtl()
    {
        var (svc, db, _) = Create();
        var msg = Msg("kuca/test", "22");

        await svc.SaveMessageAsync(msg);

        var set = Assert.Single(Calls(db, "StringSetAsync"));
        Assert.Equal($"{msg.Id}:kuca/test", set.Arguments[0].ToString());

        var json = set.Arguments[1].ToString()!;
        Assert.Contains("kuca/test", json);
        Assert.Contains("22", json);

        var ttl = set.Arguments[2]?.ToString() ?? string.Empty;
        Assert.True(ttl.Contains("3600") || ttl.Contains("01:00:00"), $"Neocekivan TTL: '{ttl}'");
    }

    [Fact]
    public async Task SaveMessageAsync_ValidTopic_IncrementsTopicCounter()
    {
        var (svc, db, logger) = Create();

        await svc.SaveMessageAsync(Msg("kuca/test"));

        var inc = Assert.Single(Calls(db, "StringIncrementAsync"));
        Assert.Equal("topic-count:kuca/test", inc.Arguments[0].ToString());
        logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.REDIS), Times.Exactly(2));
    }

    [Theory]
    [InlineData("kuca/#")]
    [InlineData("kuca/+/temp")]
    [InlineData("kuca soba")]
    public async Task SaveMessageAsync_InvalidTopic_SavesMessageButDoesNotIncrementCounter(string topic)
    {
        var (svc, db, _) = Create();

        await svc.SaveMessageAsync(Msg(topic));

        Assert.Single(Calls(db, "StringSetAsync"));
        Assert.Empty(Calls(db, "StringIncrementAsync"));
    }

    [Fact]
    public async Task SaveMessageAsync_RedisThrows_DoesNotThrowAndSkipsCounter()
    {
        var (svc, db, _) = Create((mux, d) =>
            mux.SetupSequence(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
               .Returns(d.Object)
               .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down")));

        await svc.SaveMessageAsync(Msg("kuca/test"));

        Assert.Empty(Calls(db, "StringSetAsync"));
        Assert.Empty(Calls(db, "StringIncrementAsync"));
    }
}