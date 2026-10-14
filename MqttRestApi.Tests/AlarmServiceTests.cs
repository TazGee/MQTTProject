using Microsoft.EntityFrameworkCore;
using Moq;
using MQTTnet.Protocol;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

public class AlarmServiceTests
{
    private static (AlarmService service, AppDbContext db, Mock<ILoggerService> logger) Create()
    {
        var db = TestHelpers.CreateDb();
        var logger = new Mock<ILoggerService>();
        return (new AlarmService(db, logger.Object), db, logger);
    }

    private static async Task<int> SeedTopic(AppDbContext db, string name = "kuca/test")
    {
        var topic = new Topic(name, string.Empty);
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        return topic.Id;
    }

    private static MqttMessage Msg(string payload, ValueAlarmTypes alarm) => new("kuca/test", payload, MqttQualityOfServiceLevel.AtMostOnce, false, DateTime.Now, alarm);

    [Fact]
    public async Task CreateAlarm_UnknownTopic_ThrowsBadRequest()
    {
        var (svc, db, _) = Create();

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.CreateAlarm(new CreateAlarmDto(999, AlarmTypes.Both, 10, 30)));

        Assert.Equal(0, await db.Alarms.CountAsync());
    }
    
    [Fact]
    public async Task CreateTwoAlarms_SameTopic_ThrowsBadRequest()
    {
        var (svc, db, _) = Create();
        var topicId = await SeedTopic(db);

        await svc.CreateAlarm(new CreateAlarmDto(topicId, AlarmTypes.Both, 10, 30));

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.CreateAlarm(new CreateAlarmDto(topicId, AlarmTypes.Both, 5, 50)));

        Assert.Equal(1, await db.Alarms.CountAsync());
    }

    [Theory]
    [InlineData(30, 10)]
    [InlineData(10, 10)]
    public async Task CreateAlarm_Both_LowerNotLessThanUpper_ThrowsBadRequest(int lower, int upper)
    {
        var (svc, db, _) = Create();
        var topicId = await SeedTopic(db);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            svc.CreateAlarm(new CreateAlarmDto(topicId, AlarmTypes.Both, lower, upper)));

        Assert.Equal(0, await db.Alarms.CountAsync());
    }

    [Fact]
    public async Task CreateAlarm_Valid_SavesAlarmWithGivenValues()
    {
        var (svc, db, _) = Create();
        var topicId = await SeedTopic(db);

        await svc.CreateAlarm(new CreateAlarmDto(topicId, AlarmTypes.Both, 10, 30));

        var alarm = await db.Alarms.SingleAsync();
        Assert.Equal(topicId, alarm.TopicId);
        Assert.Equal(AlarmTypes.Both, alarm.AlarmType);
        Assert.Equal(10, alarm.LowerLimit);
        Assert.Equal(30, alarm.UpperLimit);
    }

    [Fact]
    public async Task DeleteAlarm_Unknown_ThrowsBadRequest()
    {
        var (svc, _, _) = Create();

        await Assert.ThrowsAsync<BadRequestException>(() => svc.DeleteAlarm(123));
    }

    [Fact]
    public async Task DeleteAlarm_Existing_RemovesIt()
    {
        var (svc, db, _) = Create();
        var topicId = await SeedTopic(db);
        await svc.CreateAlarm(new CreateAlarmDto(topicId, AlarmTypes.Both, 10, 30));
        var id = (await db.Alarms.SingleAsync()).Id;

        await svc.DeleteAlarm(id);

        Assert.Equal(0, await db.Alarms.CountAsync());
    }

    [Fact]
    public async Task GetAllAlarms_ReturnsAllAlarms()
    {
        var (svc, db, _) = Create();
        var t1 = await SeedTopic(db, "kuca/a");
        var t2 = await SeedTopic(db, "kuca/b");
        Assert.NotEqual(t1, t2);

        db.Alarms.AddRange(
            new Alarm(t1, AlarmTypes.Both, 10, 30),
            new Alarm(t2, AlarmTypes.Minimum, 5, 50));
        await db.SaveChangesAsync();

        var result = await svc.GetAllAlarms();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAlarms_Empty_ReturnsEmptyList()
    {
        var (svc, _, _) = Create();

        Assert.Empty(await svc.GetAllAlarms());
    }

    [Fact]
    public async Task GetAllAlarmedMessages_ReturnsOnlyMessagesWithAlarm()
    {
        var (svc, db, _) = Create();
        db.Messages.AddRange(
            Msg("20", ValueAlarmTypes.NONE),
            Msg("35", ValueAlarmTypes.ALARM_HIGH),
            Msg("5", ValueAlarmTypes.ALARM_LOW));
        await db.SaveChangesAsync();

        var result = await svc.GetAllAlarmedMessages();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, m => m.AlarmType == ValueAlarmTypes.NONE);
    }
}