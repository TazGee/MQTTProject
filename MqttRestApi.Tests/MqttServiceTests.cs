using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MQTTnet;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

/// <summary>
/// Testovi za MqttService
/// </summary>
public class MqttServiceTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublishRegex"] = @"^[^/#+\s]+(?:/[^/#+\s]+)*$",
                ["SubscribeRegex"] = @"^[^/#+\s]+(?:/(?:[^/#+\s]+|\+))*?(?:/#)?$"
            })
            .Build();
    
    private static (MqttService service, Mock<IMqttClient> client) CreateService()
    {
        var client = new Mock<IMqttClient>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var redis = new Mock<RedisService>();

        var service = new MqttService(scopeFactory.Object, redis.Object, BuildConfig(), client.Object);
        return (service, client);
    }
    
    [Fact]
    public async Task PublishAsync_ReturnsFalse_OnBadInput()
    {
        var (service, client) = CreateService();

        var result = await service.PublishAsync("bla bla topic!", "x", 0);

        Assert.False(result);
    }

    [Fact]
    public async Task PublishAsync_ReturnsTrue_OnGoodInput()
    {
        var (service, client) = CreateService();

        var result = await service.PublishAsync("test/test", "x", 0);

        Assert.True(result);
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsFalse_OnBadInput()
    {
        var (service, client) = CreateService();

        var result = await service.SubscribeAsync("bla bla topic!");

        Assert.False(result);
    }

    [Theory]
    [InlineData("test/test")]
    [InlineData("test/+/test")]
    [InlineData("test/+/#")]
    public async Task SubscribeAsync_ReturnsTrue_OnGoodInput(string topic)
    {
        var (service, client) = CreateService();

        var result = await service.SubscribeAsync(topic);

        Assert.True(result);
    }

    [Fact]
    public async Task SubscribeAsync_ReturnsFalse_OnBadInputMultiLevelWildcard()
    {
        var (service, client) = CreateService();

        var result = await service.SubscribeAsync("test/#/test");

        Assert.False(result);
    }
}