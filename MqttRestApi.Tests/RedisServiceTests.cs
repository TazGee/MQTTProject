using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Services;
using StackExchange.Redis;

namespace MqttRestApi.Tests;

public class RedisServiceTests
{
    private static (RedisService svc, Mock<IDatabase> db) Create()
    {
        var db = new Mock<IDatabase>();
        var mux = new Mock<IConnectionMultiplexer>();
        mux.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(db.Object);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublishRegex"] = @"^[^/#+\s]+(?:/[^/#+\s]+)*$"
            })
            .Build();

        var scopeFactory = new ServiceCollection().BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();

        return (new RedisService(mux.Object, config, scopeFactory), db);
    }

    [Fact]
    public async Task AddAsync_AddsUserToTopicSetAndTopicToUserSet()
    {
        var (svc, db) = Create();

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
        var (svc, db) = Create();
        db.Setup(d => d.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(false);

        await Assert.ThrowsAsync<BadRequestException>(() => svc.RemoveAsync("kuca/test", 7));
    }

    [Fact]
    public async Task RemoveAsync_BothRemovalsSucceed_DoesNotThrow()
    {
        var (svc, db) = Create();
        db.Setup(d => d.SetRemoveAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(true);

        await svc.RemoveAsync("kuca/test", 7);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsSubscribedAsync_ReturnsRedisAnswer(bool contains)
    {
        var (svc, db) = Create();
        db.Setup(d => d.SetContainsAsync(
                It.Is<RedisKey>(k => k == "topic:kuca/test:users"),
                It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(contains);

        Assert.Equal(contains, await svc.IsSubscribedAsync("kuca/test", 7));
    }

    [Fact]
    public async Task GetUserTopicsAsync_ReturnsTopicNames()
    {
        var (svc, db) = Create();
        db.Setup(d => d.SetMembersAsync(It.Is<RedisKey>(k => k == "user:7:topics"), It.IsAny<CommandFlags>()))
          .ReturnsAsync(new RedisValue[] { "kuca/a", "kuca/b" });

        var result = await svc.GetUserTopicsAsync(7);

        Assert.Equal(new[] { "kuca/a", "kuca/b" }, result);
    }

    [Fact]
    public async Task GetTopicCountAsync_KeyExists_ReturnsCount()
    {
        var (svc, db) = Create();
        db.Setup(d => d.StringGetAsync(It.Is<RedisKey>(k => k == "topic-count:kuca/test"), It.IsAny<CommandFlags>()))
          .ReturnsAsync((RedisValue)5);

        Assert.Equal(5, await svc.GetTopicCountAsync("kuca/test"));
    }

    [Fact]
    public async Task GetTopicCountAsync_KeyMissing_ReturnsZero()
    {
        var (svc, db) = Create();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
          .ReturnsAsync(RedisValue.Null);

        Assert.Equal(0, await svc.GetTopicCountAsync("kuca/test"));
    }

    [Fact]
    public async Task GetTopicCountAsync_RedisThrows_ReturnsZero()
    {
        var (svc, db) = Create();
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
          .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        Assert.Equal(0, await svc.GetTopicCountAsync("kuca/test"));
    }
}