using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MQTTnet;
using MQTTnet.Packets;
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
                ["SubscribeRegex"] = @"^(?:#|(?:[^/#+\s]+|\+)(?:/(?:[^/#+\s]+|\+))*(?:/#)?)$"
            })
            .Build();
    
    private static (MqttService service, Mock<IMqttClient> client) CreateService()
    {
        var client = new Mock<IMqttClient>();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var redis = new Mock<IRedisService>();

        client
            .Setup(c => c.SubscribeAsync(
                It.IsAny<MqttClientSubscribeOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientSubscribeResult(
                0,
                new List<MqttClientSubscribeResultItem>
                {
                    new MqttClientSubscribeResultItem(
                        new MqttTopicFilter { Topic = "x" },
                        MqttClientSubscribeResultCode.GrantedQoS0)
                },
                string.Empty,
                new List<MqttUserProperty>()));

        var service = new MqttService(scopeFactory.Object, redis.Object, BuildConfig(), client.Object);
        return (service, client);
    }
    
    
}