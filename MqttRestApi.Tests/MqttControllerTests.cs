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
        mqtt.Setup(m => m.SubscribeAsync("test/topic")).ReturnsAsync(true);

        var result = await controller.Subscribe(new SubscribeRequestDto { Topic = "test/topic" });

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Subscribe_ReturnsBadRequest_WhenServiceFails()
    {
        var (controller, mqtt, _) = CreateController();
        mqtt.Setup(m => m.SubscribeAsync(It.IsAny<string>())).ReturnsAsync(false);

        var result = await controller.Subscribe(new SubscribeRequestDto { Topic = "bla bla" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Publish_ReturnsOk_WhenServiceSucceeds()
    {
        var (controller, mqtt, _) = CreateController();
        mqtt.Setup(m => m.PublishAsync("test/topic", "x", MqttQualityOfServiceLevel.AtLeastOnce))
            .ReturnsAsync(true);

        var result = await controller.Publish(new PublishRequestDto
        {
            Topic = "test/topic",
            Payload = "x",
            QoS = MqttQualityOfServiceLevel.AtLeastOnce
        });

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Publish_ReturnsBadRequest_WhenServiceFails()
    {
        var (controller, mqtt, _) = CreateController();
        mqtt.Setup(m => m.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MqttQualityOfServiceLevel>()))
            .ReturnsAsync(false);

        var result = await controller.Publish(new PublishRequestDto { Topic = "bad", Payload = "x" });

        Assert.IsType<BadRequestObjectResult>(result);
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