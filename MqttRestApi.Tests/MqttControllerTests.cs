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
    public async Task Subscribe_ReturnsOk_WhenServiceSucceeds()
    {
        var (controller, mqtt, _) = CreateController();
        mqtt.Setup(m => m.SubscribeAsync()).ReturnsAsync(true);

        var result = await controller.Subscribe(new SubscribeRequestDto { Topic = "test/topic" });

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public void GetMessages_ReturnsOk_WithMessagesFromService()
    {
        var (controller, mqtt, _) = CreateController();
        var messages = new List<MqttMessage>();
        mqtt.Setup(m => m.GetMessages()).Returns(messages);

        var result = controller.GetMessages();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(messages, ok.Value);
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