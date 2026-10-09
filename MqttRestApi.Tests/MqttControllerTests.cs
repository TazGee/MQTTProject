using Microsoft.AspNetCore.Mvc;
using Moq;
using MQTTnet.Protocol;
using MQTTRestApi.Controllers;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MqttRestApi.Tests;

/// <summary>
/// Testovi za MqttController
/// </summary>
public class MqttControllerTests
{
    private static (MqttController controller, Mock<IMqttService> mqtt, Mock<IRedisService> redis) CreateController()
    {
        var mqtt = new Mock<IMqttService>();
        var redis = new Mock<IRedisService>();
        var controller = new MqttController(mqtt.Object, redis.Object);
        return (controller, mqtt, redis);
    }

    [Fact]
    public async Task GetTopicStats_DecodesTopic_AndReturnsCount()
    {
        var (controller, _, redis) = CreateController();
        redis.Setup(r => r.GetTopicCountAsync("test/topic")).ReturnsAsync(5);

        var result = await controller.GetTopicStats("test%2Ftopic");

        Assert.IsType<OkObjectResult>(result);
        redis.Verify(r => r.GetTopicCountAsync("test/topic"), Times.Once);
    }
}