using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Hubs;
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

    private sealed class HandlerBox
    {
        public Func<MqttApplicationMessageReceivedEventArgs, Task>? Handler { get; set; }
    }

    private sealed class Fixture
    {
        public required MqttService Service { get; init; }
        public required Mock<IMqttClient> Client { get; init; }
        public required Mock<IRedisService> Redis { get; init; }
        public required Mock<IClientProxy> HubProxy { get; init; }
        public required IServiceProvider Provider { get; init; }
        public required HandlerBox Box { get; init; }

        public async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> action)
        {
            using var scope = Provider.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        public async Task WithDb(Func<AppDbContext, Task> action)
        {
            using var scope = Provider.CreateScope();
            await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }
    }

    private static Fixture CreateService()
    {
        var client = new Mock<IMqttClient>();
        var redis = new Mock<IRedisService>();
        var box = new HandlerBox();

        client
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientSubscribeResult(
                0,
                new List<MqttClientSubscribeResultItem>
                {
                    new(new MqttTopicFilter { Topic = "x" }, MqttClientSubscribeResultCode.GrantedQoS0)
                },
                string.Empty,
                new List<MqttUserProperty>()));

        client
            .SetupAdd(c => c.ApplicationMessageReceivedAsync += It.IsAny<Func<MqttApplicationMessageReceivedEventArgs, Task>>())
            .Callback<Func<MqttApplicationMessageReceivedEventArgs, Task>>(h => box.Handler = h);

        var proxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.All).Returns(proxy.Object);
        var hub = new Mock<IHubContext<MessagesHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped(_ => new Mock<ILoggerService>().Object);
        var provider = services.BuildServiceProvider();

        var service = new MqttService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            redis.Object, BuildConfig(), client.Object, hub.Object);

        return new Fixture
        {
            Service = service,
            Client = client,
            Redis = redis,
            HubProxy = proxy,
            Provider = provider,
            Box = box
        };
    }

    [Theory]
    [InlineData("kuca/#/soba")]
    [InlineData("kuca soba")]
    [InlineData("")]
    public async Task SubscribeAsync_InvalidTopic_ThrowsBadRequest(string topic)
    {
        var f = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.SubscribeAsync(topic));

        f.Client.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_ValidTopic_SubscribesAndSavesTopic()
    {
        var f = CreateService();

        await f.Service.SubscribeAsync("kuca/soba/temperatura");

        f.Client.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(await f.WithDb(db => db.Topics.AnyAsync(t => t.Name == "kuca/soba/temperatura")));
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateTopic_ThrowsBadRequest()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/test");

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.SubscribeAsync("kuca/test"));

        Assert.Equal(1, await f.WithDb(db => db.Topics.CountAsync()));
    }

    [Fact]
    public async Task SubscribeAsync_BrokerRejects_ThrowsAndDoesNotSaveTopic()
    {
        var f = CreateService();
        f.Client
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientSubscribeResult(
                0,
                new List<MqttClientSubscribeResultItem>
                {
                    new(new MqttTopicFilter { Topic = "x" }, MqttClientSubscribeResultCode.NotAuthorized)
                },
                "denied",
                new List<MqttUserProperty>()));

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.SubscribeAsync("kuca/test"));

        Assert.Equal(0, await f.WithDb(db => db.Topics.CountAsync()));
    }

    [Fact]
    public async Task SubscribeUserAsync_UnknownTopic_ThrowsNotFound()
    {
        var f = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = 999 }, userId: 1));
    }

    [Fact]
    public async Task SubscribeUserAsync_Valid_SavesSubscriptionAndUpdatesRedis()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/test");
        var topicId = await f.WithDb(db => db.Topics.Select(t => t.Id).FirstAsync());

        await f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, userId: 1);

        f.Redis.Verify(r => r.AddAsync("kuca/test", 1), Times.Once);
        Assert.True(await f.WithDb(db => db.UserSubscriptions.AnyAsync(s => s.UserId == 1 && s.TopicId == topicId)));
    }

    [Fact]
    public async Task SubscribeUserAsync_AlreadySubscribed_ThrowsConflict()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/test");
        var topicId = await f.WithDb(db => db.Topics.Select(t => t.Id).FirstAsync());
        var request = new SubscribeRequestDto { TopicId = topicId };
        await f.Service.SubscribeUserAsync(request, 1);

        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SubscribeUserAsync(request, 1));
    }

    [Fact]
    public async Task SubscribeUserAsync_RedisFails_RollsBackSubscription()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/test");
        var topicId = await f.WithDb(db => db.Topics.Select(t => t.Id).FirstAsync());
        f.Redis.Setup(r => r.AddAsync(It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(new Exception("redis down"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, 1));

        Assert.Equal(0, await f.WithDb(db => db.UserSubscriptions.CountAsync()));
    }

    [Fact]
    public async Task UnsubscribeUserAsync_NotSubscribed_ThrowsNotFound()
    {
        var f = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => f.Service.UnsubscribeUserAsync(1, 1));
    }

    private static MqttMessage Msg(string topic, string payload, DateTime at,
        ValueAlarmTypes alarm = ValueAlarmTypes.NONE) =>
        new(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, at, alarm);

    [Fact]
    public async Task GetMessages_ReturnsOnlyMessagesMatchingUserSubscriptions()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/#");
        var topicId = await f.WithDb(db => db.Topics.Select(t => t.Id).FirstAsync());
        await f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, 1);

        await f.WithDb(async db =>
        {
            db.Messages.AddRange(
                Msg("kuca/soba/temperatura", "20", DateTime.Now),
                Msg("kuca/kuhinja/vlaznost", "40", DateTime.Now),
                Msg("garaza/vrata", "1", DateTime.Now));
            await db.SaveChangesAsync();
        });

        var result = await f.Service.GetMessages(1);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.StartsWith("kuca/", m.Topic));
    }

    [Fact]
    public async Task GetFilteredMessages_ReturnsOnlyMessagesNewerThanGivenMinutes()
    {
        var f = CreateService();
        await f.Service.SubscribeAsync("kuca/test");
        var topicId = await f.WithDb(db => db.Topics.Select(t => t.Id).FirstAsync());
        await f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, 1);

        await f.WithDb(async db =>
        {
            db.Messages.AddRange(
                Msg("kuca/test", "novo", DateTime.Now.AddMinutes(-1)),
                Msg("kuca/test", "staro", DateTime.Now.AddMinutes(-30)));
            await db.SaveChangesAsync();
        });

        var result = await f.Service.GetFilteredMessages("kuca/test", minutes: 5);

        Assert.Single(result);
        Assert.Equal("novo", result[0].Payload);
    }

    [Fact]
    public async Task ResubscribeToFailed_NoFailedTopics_ThrowsBadRequest()
    {
        var f = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.ResubscribeToFailed());
        Assert.Empty(f.Service.GetFailedTopics());
    }

    [Fact]
    public async Task DeviceSuggestValueChange_NotConnected_DoesNotPublish()
    {
        var f = CreateService();

        await f.Service.DeviceSuggestValueChange("kuca/soba/temperatura/updateValue", 22.5f);

        f.Client.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task RaiseMessage(Fixture f, string topic, string payload)
    {
        var appMessage = new MqttApplicationMessageBuilder()
            .WithTopic(topic).WithPayload(payload).Build();

        var args = new MqttApplicationMessageReceivedEventArgs(
            "test-client", appMessage, new MqttPublishPacket(), (_, _) => Task.CompletedTask);

        var handler = f.Box.Handler
            ?? throw new InvalidOperationException("Handler nije registrovan.");

        await handler(args);
    }

    private static async Task SeedTopicWithAlarm(Fixture f, string topic, int lower, int upper)
    {
        await f.WithDb(async db =>
        {
            var t = new Topic(topic, string.Empty);
            db.Topics.Add(t);
            await db.SaveChangesAsync();
            db.Alarms.Add(new Alarm
            {
                TopicId = t.Id,
                AlarmType = AlarmTypes.Both,
                LowerLimit = lower,
                UpperLimit = upper,
                CreatedAt = DateTime.Now
            });
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task MessageHandler_ValueAboveUpperLimit_SavesAsAlarmHighAndNotifiesHub()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", lower: 10, upper: 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "35");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.ALARM_HIGH, saved.AlarmType);

        f.HubProxy.Verify(p => p.SendCoreAsync("Message",
            It.Is<object?[]>(a => (string)a[0]! == "kuca/soba/temperatura"
                               && (string)a[2]! == "ALARM_HIGH"),
            It.IsAny<CancellationToken>()), Times.Once);
        f.Redis.Verify(r => r.SaveMessageAsync(It.IsAny<MqttMessage>()), Times.Once);
    }

    [Fact]
    public async Task MessageHandler_ValueBelowLowerLimit_SavesAsAlarmLow()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", lower: 10, upper: 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "5");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.ALARM_LOW, saved.AlarmType);
    }

    [Fact]
    public async Task MessageHandler_ValueInRange_SavesWithoutAlarm()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", lower: 10, upper: 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "20");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
    }
}