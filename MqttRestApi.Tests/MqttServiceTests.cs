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
public partial class MqttServiceTests
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
        public required Mock<ILoggerService> Logger { get; init; }
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
        var logger = new Mock<ILoggerService>();
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
        services.AddScoped(_ => logger.Object);
        var provider = services.BuildServiceProvider();

        var service = new MqttService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            redis.Object, BuildConfig(), client.Object, hub.Object);

        return new Fixture
        {
            Service = service,
            Client = client,
            Redis = redis,
            Logger = logger,
            HubProxy = proxy,
            Provider = provider,
            Box = box
        };
    }

    private static MqttMessage Msg(string topic, string payload, DateTime at,
        ValueAlarmTypes alarm = ValueAlarmTypes.NONE) =>
        new(topic, payload, MqttQualityOfServiceLevel.AtMostOnce, false, at, alarm);

    private static async Task<int> SubscribeTopic(Fixture f, string topic)
    {
        await f.Service.SubscribeAsync(topic);
        return await f.WithDb(db => db.Topics.Where(t => t.Name == topic).Select(t => t.Id).SingleAsync());
    }

    private static async Task<int> SubscribeUserToTopic(Fixture f, string topic, int userId)
    {
        var topicId = await SubscribeTopic(f, topic);
        await f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, userId);
        return topicId;
    }

    [Theory]
    [InlineData("kuca/#/soba")]
    [InlineData("kuca soba")]
    [InlineData("")]
    public async Task SubscribeAsync_InvalidTopic_ThrowsBadRequest(string topic)
    {
        var f = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.SubscribeAsync(topic));

        f.Client.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubscribeAsync_ValidTopic_SubscribesAndSavesTopic()
    {
        var f = CreateService();

        await f.Service.SubscribeAsync("kuca/soba/temperatura");

        f.Client.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Once);
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
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MqttClientSubscribeResult(
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
        var topicId = await SubscribeTopic(f, "kuca/test");

        await f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, userId: 1);

        f.Redis.Verify(r => r.AddAsync("kuca/test", 1), Times.Once);
        Assert.True(await f.WithDb(db => db.UserSubscriptions.AnyAsync(s => s.UserId == 1 && s.TopicId == topicId)));
    }

    [Fact]
    public async Task SubscribeUserAsync_AlreadySubscribed_ThrowsConflict()
    {
        var f = CreateService();
        var topicId = await SubscribeUserToTopic(f, "kuca/test", 1);

        await Assert.ThrowsAsync<ConflictException>(() =>
            f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, 1));
    }

    [Fact]
    public async Task SubscribeUserAsync_RedisFails_RollsBackAndThrowsBadRequest()
    {
        var f = CreateService();
        var topicId = await SubscribeTopic(f, "kuca/test");
        f.Redis.Setup(r => r.AddAsync(It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(new Exception("redis down"));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            f.Service.SubscribeUserAsync(new SubscribeRequestDto { TopicId = topicId }, 1));

        Assert.Equal(0, await f.WithDb(db => db.UserSubscriptions.CountAsync()));
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ERROR), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeUserAsync_NotSubscribed_ThrowsNotFound()
    {
        var f = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => f.Service.UnsubscribeUserAsync(1, 1));
    }

    [Fact]
    public async Task UnsubscribeUserAsync_Valid_RemovesSubscriptionAndUpdatesRedis()
    {
        var f = CreateService();
        var topicId = await SubscribeUserToTopic(f, "kuca/test", 1);

        await f.Service.UnsubscribeUserAsync(topicId, 1);

        Assert.Equal(0, await f.WithDb(db => db.UserSubscriptions.CountAsync()));
        f.Redis.Verify(r => r.RemoveAsync("kuca/test", 1), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeUserAsync_RedisFails_RestoresSubscriptionAndThrows()
    {
        var f = CreateService();
        var topicId = await SubscribeUserToTopic(f, "kuca/test", 1);
        f.Redis.Setup(r => r.RemoveAsync(It.IsAny<string>(), It.IsAny<int>()))
               .ThrowsAsync(new Exception("redis down"));

        await Assert.ThrowsAsync<NotFoundException>(() => f.Service.UnsubscribeUserAsync(topicId, 1));

        Assert.Equal(1, await f.WithDb(db => db.UserSubscriptions.CountAsync()));
    }

    [Fact]
    public async Task GetAvailableTopics_ReturnsAllTopics()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/a");
        await SubscribeTopic(f, "kuca/b");

        var result = await f.Service.GetAvailableTopics();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task MyTopics_ReturnsOnlyTopicsOfThatUser()
    {
        var f = CreateService();
        var t1 = await SubscribeUserToTopic(f, "kuca/a", 1);
        await SubscribeTopic(f, "kuca/b");

        var result = await f.Service.MyTopics(1);

        Assert.Single(result);
        Assert.Equal(t1, result[0].TopicId);
    }

    [Theory]
    [InlineData("kuca/#")]
    [InlineData("kuca/+/temperatura")]
    public async Task GetMessages_ReturnsOnlyMessagesMatchingUserSubscriptions(string value)
    {
        var f = CreateService();
        await SubscribeUserToTopic(f, value, 1);

        await f.WithDb(async db =>
        {
            db.Messages.AddRange(
                Msg("kuca/soba/temperatura", "20", DateTime.Now),
                Msg("kuca/kuhinja/temperatura", "24", DateTime.Now),
                Msg("garaza/vrata", "1", DateTime.Now));
            await db.SaveChangesAsync();
        });

        var result = await f.Service.GetMessages(1);

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.StartsWith("kuca/", m.Topic));
    }

    [Fact]
    public async Task GetAllMessages_ReturnsEverythingNewestFirst()
    {
        var f = CreateService();
        await f.WithDb(async db =>
        {
            db.Messages.AddRange(
                Msg("a/b", "1", DateTime.Now),
                Msg("c/d", "2", DateTime.Now));
            await db.SaveChangesAsync();
        });

        var result = await f.Service.GetAllMessages();

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Id > result[1].Id);
    }

    [Fact]
    public async Task GetFilteredMessages_UserNotSubscribed_ThrowsUnauthorized()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/test");

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            f.Service.GetFilteredMessages(1, "kuca/test", 5));
    }

    [Fact]
    public async Task GetFilteredMessages_OtherUserSubscribed_StillThrowsUnauthorized()
    {
        var f = CreateService();
        await SubscribeUserToTopic(f, "kuca/test", 2);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            f.Service.GetFilteredMessages(1, "kuca/test", 5));
    }

    [Fact]
    public async Task GetFilteredMessages_ReturnsOnlyRecentMessagesOfThatTopic()
    {
        var f = CreateService();
        await SubscribeUserToTopic(f, "kuca/test", 1);

        await f.WithDb(async db =>
        {
            db.Messages.AddRange(
                Msg("kuca/test", "novo", DateTime.Now.AddMinutes(-1)),
                Msg("kuca/test", "staro", DateTime.Now.AddMinutes(-30)),
                Msg("kuca/drugo", "tudje", DateTime.Now.AddMinutes(-1)));
            await db.SaveChangesAsync();
        });

        var result = await f.Service.GetFilteredMessages(1, "kuca/test", minutes: 5);

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
    public async Task ResubscribeToAll_BrokerRejects_TopicIsAddedToFailedList()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/test");
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

        await f.Service.ResubscribeToAll();

        var failed = f.Service.GetFailedTopics();
        Assert.Single(failed);
        Assert.Equal("kuca/test", failed[0].Name);
    }

    [Fact]
    public async Task ResubscribeToFailed_BrokerAcceptsLater_ReturnsSuccessAndClearsFailedList()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/test");
        
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

        await f.Service.ResubscribeToAll();
        Assert.Single(f.Service.GetFailedTopics());
        
        f.Client
            .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MqttClientSubscribeResult(
                0,
                new List<MqttClientSubscribeResultItem>
                {
                    new(new MqttTopicFilter { Topic = "x" }, MqttClientSubscribeResultCode.GrantedQoS0)
                },
                string.Empty,
                new List<MqttUserProperty>()));

        var results = await f.Service.ResubscribeToFailed();

        Assert.Single(results);
        Assert.Equal("Uspesan resubscribe na kuca/test!", results[0]);
        Assert.Empty(f.Service.GetFailedTopics());
    }
    
    [Fact]
    public async Task ForceReconnect_WhenNotConnected_ConnectsAndResubscribes()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/test");
        f.Client.Invocations.Clear();

        await f.Service.ForceReconnect();

        f.Client.Verify(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        f.Client.Verify(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ForceReconnect_WhenAlreadyConnected_ThrowsBadRequest()
    {
        var f = CreateService();
        await f.Service.ForceReconnect();

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.ForceReconnect());
    }

    [Fact]
    public async Task ForceReconnect_ConnectFails_ThrowsBadRequest()
    {
        var f = CreateService();
        f.Client.Setup(c => c.ConnectAsync(It.IsAny<MqttClientOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("nema brokera"));

        await Assert.ThrowsAsync<BadRequestException>(() => f.Service.ForceReconnect());
    }

    [Fact]
    public async Task DeviceSuggestValueChange_NotConnected_DoesNotPublish()
    {
        var f = CreateService();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            f.Service.DeviceSuggestValueChange("kuca/test", 1f));

        f.Client.Verify(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeviceSuggestValueChange_Connected_PublishesToTopic()
    {
        var f = CreateService();
        await f.Service.ForceReconnect();

        await f.Service.DeviceSuggestValueChange("kuca/test", 22.5f);

        f.Client.Verify(c => c.PublishAsync(
            It.Is<MqttApplicationMessage>(m => m.Topic == "kuca/test/updateValue"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeviceSuggestValueChange_PublishFails_ThrowsBadRequest()
    {
        var f = CreateService();
        await f.Service.ForceReconnect();
        f.Client.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("greska"));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            f.Service.DeviceSuggestValueChange("kuca/test", 1f));
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

    private static async Task SeedTopicWithAlarm(Fixture f, string topic, AlarmTypes type, int lower, int upper)
    {
        await f.WithDb(async db =>
        {
            var t = new Topic(topic, string.Empty);
            db.Topics.Add(t);
            await db.SaveChangesAsync();
            db.Alarms.Add(new Alarm(t.Id, type, lower, upper));
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task MessageHandler_ValueAboveUpperLimit_SavesAsAlarmHighAndNotifiesHub()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "35");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.ALARM_HIGH, saved.AlarmType);

        f.HubProxy.Verify(p => p.SendCoreAsync("Message",
            It.Is<object?[]>(a => (string)a[0]! == "kuca/soba/temperatura"
                               && (string)a[1]! == "35"
                               && (string)a[2]! == "ALARM_HIGH"),
            It.IsAny<CancellationToken>()), Times.Once);
        f.Redis.Verify(r => r.SaveMessageAsync(It.IsAny<MqttMessage>()), Times.Once);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_HIGH), Times.Once);
    }

    [Fact]
    public async Task MessageHandler_ValueBelowLowerLimit_SavesAsAlarmLow()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "5");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.ALARM_LOW, saved.AlarmType);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_LOW), Times.Once);
    }

    [Fact]
    public async Task MessageHandler_ValueInRange_SavesWithoutAlarm()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "20");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_HIGH), Times.Never);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_LOW), Times.Never);
    }

    [Fact]
    public async Task MessageHandler_DecimalPayload_IsComparedAsNumber()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "30.5");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.ALARM_HIGH, saved.AlarmType);
    }

    [Fact]
    public async Task MessageHandler_MaximumOnlyAlarm_IgnoresLowValues()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Maximum, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "5");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
    }

    [Fact]
    public async Task MessageHandler_MinimumOnlyAlarm_IgnoresHighValues()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Minimum, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "35");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
    }

    [Fact]
    public async Task MessageHandler_NoAlarmForTopic_SavesEvenWithNonNumericPayload()
    {
        var f = CreateService();
        await SubscribeTopic(f, "kuca/test");

        await RaiseMessage(f, "kuca/test", "nije broj");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal("nije broj", saved.Payload);
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
    }

    [Fact]
    public async Task MessageHandler_AlarmOnTopic_NonNumericPayload_SavesWithoutAlarmAndNotifiesHub()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/soba/temperatura", "nije broj");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal("nije broj", saved.Payload);
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);

        f.HubProxy.Verify(p => p.SendCoreAsync("Message",
            It.Is<object?[]>(a => (string)a[0]! == "kuca/soba/temperatura"
                                  && (string)a[1]! == "nije broj"
                                  && (string)a[2]! == "NONE"),
            It.IsAny<CancellationToken>()), Times.Once);
        f.Redis.Verify(r => r.SaveMessageAsync(It.IsAny<MqttMessage>()), Times.Once);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_HIGH), Times.Never);
        f.Logger.Verify(l => l.LogMessage(It.IsAny<string>(), LogTypes.ALARM_LOW), Times.Never);
    }
    
    [Fact]
    public async Task MessageHandler_AlarmOnOtherTopic_DoesNotAffectMessage()
    {
        var f = CreateService();
        await SeedTopicWithAlarm(f, "kuca/soba/temperatura", AlarmTypes.Both, 10, 30);

        await RaiseMessage(f, "kuca/kuhinja/temperatura", "99");

        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(ValueAlarmTypes.NONE, saved.AlarmType);
    }
}