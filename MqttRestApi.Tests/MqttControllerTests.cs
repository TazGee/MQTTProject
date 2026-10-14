using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MQTTnet.Protocol;
using MQTTRestApi.Controllers;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MqttRestApi.Tests;

public class MqttControllerTests
{
    private static (MqttController controller, Mock<IMqttService> mqtt, Mock<IRedisService> redis) Create(int userId = 1)
    {
        var mqtt = new Mock<IMqttService>();
        var redis = new Mock<IRedisService>();
        var controller = new MqttController(mqtt.Object, redis.Object).WithUser(userId);
        return (controller, mqtt, redis);
    }

    private static MqttMessageDto Dto() => new(1, "kuca/test", "20", MqttQualityOfServiceLevel.AtMostOnce, false, DateTime.Now, ValueAlarmTypes.NONE);

    [Theory]
    [InlineData(nameof(MqttController.Subscribe))]
    [InlineData(nameof(MqttController.FailedSubscriptions))]
    [InlineData(nameof(MqttController.ResubscribeToFailed))]
    [InlineData(nameof(MqttController.GetAllMessages))]
    [InlineData(nameof(MqttController.ReconnectToServer))]
    [InlineData(nameof(MqttController.SuggestDeviceValueChange))]
    public void AdminEndpoints_RequireAdminRole(string method)
    {
        var attrs = typeof(MqttController)
            .GetMethod(method)!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>();

        Assert.Contains(attrs, a => a.Roles == "Admin");
    }

    [Fact]
    public async Task Subscribe_CallsServiceWithTopic()
    {
        var (controller, mqtt, _) = Create();

        var result = await controller.Subscribe(new SubscribeRequestDto { Topic = "kuca/test" });

        mqtt.Verify(m => m.SubscribeAsync("kuca/test"), Times.Once);
        Assert.Equal("Subscribe uspesan!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task UserSubscribe_UsesUserIdFromClaims()
    {
        var (controller, mqtt, _) = Create(userId: 8);
        var request = new SubscribeRequestDto { TopicId = 3 };

        var result = await controller.UserSubscribe(request);

        mqtt.Verify(m => m.SubscribeUserAsync(request, 8), Times.Once);
        Assert.Equal("User subscribe uspesan!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task UserUnsubscribe_UsesUserIdFromClaims()
    {
        var (controller, mqtt, _) = Create(userId: 8);

        var result = await controller.UserUnsubscribe(3);

        mqtt.Verify(m => m.UnsubscribeUserAsync(3, 8), Times.Once);
        Assert.Equal("User unsubscribe uspesan!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task MyTopics_Empty_ReturnsMessage()
    {
        var (controller, mqtt, _) = Create();
        mqtt.Setup(m => m.MyTopics(1)).ReturnsAsync(new List<TopicListDto>());

        var result = await controller.MyTopics();

        Assert.Equal("Lista vasih topic-a je prazna.", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task MyTopics_NotEmpty_ReturnsList()
    {
        var (controller, mqtt, _) = Create();
        var list = new List<TopicListDto> { new(1, "kuca/test") };
        mqtt.Setup(m => m.MyTopics(1)).ReturnsAsync(list);

        var result = await controller.MyTopics();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public void FailedSubscriptions_ReturnsListFromService()
    {
        var (controller, mqtt, _) = Create();
        var list = new List<Topic> { new("kuca/test", string.Empty) };
        mqtt.Setup(m => m.GetFailedTopics()).Returns(list);

        var result = controller.FailedSubscriptions();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task ResubscribeToFailed_ReturnsResultsFromService()
    {
        var (controller, mqtt, _) = Create();
        var list = new List<string> { "Uspesan resubscribe na kuca/test!" };
        mqtt.Setup(m => m.ResubscribeToFailed()).ReturnsAsync(list);

        var result = await controller.ResubscribeToFailed();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetMessages_Empty_ReturnsMessage()
    {
        var (controller, mqtt, _) = Create();
        mqtt.Setup(m => m.GetMessages(1)).ReturnsAsync(new List<MqttMessageDto>());

        var result = await controller.GetMessages();

        Assert.Equal("Nemate dostupnih poruka.", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetMessages_NotEmpty_ReturnsMessagesForCurrentUser()
    {
        var (controller, mqtt, _) = Create(userId: 5);
        var list = new List<MqttMessageDto> { Dto() };
        mqtt.Setup(m => m.GetMessages(5)).ReturnsAsync(list);

        var result = await controller.GetMessages();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllMessages_Empty_ReturnsMessage()
    {
        var (controller, mqtt, _) = Create();
        mqtt.Setup(m => m.GetAllMessages()).ReturnsAsync(new List<MqttMessageDto>());

        var result = await controller.GetAllMessages();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<MqttMessageDto>>(ok.Value));
    }

    [Fact]
    public async Task GetMessagesFiltered_PassesTopicAndMinutes()
    {
        var (controller, mqtt, _) = Create();
        var list = new List<MqttMessageDto> { Dto() };
        mqtt.Setup(m => m.GetFilteredMessages(1, "kuca/test", 10u)).ReturnsAsync(list);

        var result = await controller.GetMessagesFiltered("kuca/test", 10);

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAvailableTopics_Empty_ReturnsMessage()
    {
        var (controller, mqtt, _) = Create();
        mqtt.Setup(m => m.GetAvailableTopics()).ReturnsAsync(new List<Topic>());

        var result = await controller.GetAvailableTopics();

        Assert.Equal("Nema dostupnih topic-a", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetStats_ReturnsCountsFromRedis()
    {
        var (controller, _, redis) = Create();
        var counts = new Dictionary<string, int> { ["kuca/test"] = 4 };
        redis.Setup(r => r.GetAllTopicsCountAsync()).ReturnsAsync(counts);

        var result = await controller.GetStats();

        Assert.Same(counts, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetTopicStats_DecodesTopicAndReturnsCount()
    {
        var (controller, _, redis) = Create();
        redis.Setup(r => r.GetTopicCountAsync("kuca/soba/temperatura")).ReturnsAsync(7);

        var result = await controller.GetTopicStats("kuca%2Fsoba%2Ftemperatura");

        var ok = Assert.IsType<OkObjectResult>(result);
        var type = ok.Value!.GetType();
        Assert.Equal("kuca/soba/temperatura", type.GetProperty("Topic")!.GetValue(ok.Value));
        Assert.Equal(7, type.GetProperty("Count")!.GetValue(ok.Value));
    }

    [Fact]
    public async Task ReconnectToServer_CallsForceReconnect()
    {
        var (controller, mqtt, _) = Create();

        var result = await controller.ReconnectToServer();

        mqtt.Verify(m => m.ForceReconnect(), Times.Once);
        Assert.Equal("Force reconnect uspesan!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task SuggestDeviceValueChange_CallsServiceWithTopicAndValue()
    {
        var (controller, mqtt, _) = Create();

        var result = await controller.SuggestDeviceValueChange("kuca/test/updateValue", 22.5f);

        mqtt.Verify(m => m.DeviceSuggestValueChange("kuca/test/updateValue", 22.5f), Times.Once);
        Assert.IsType<OkObjectResult>(result);
    }
}