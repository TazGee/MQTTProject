using Microsoft.EntityFrameworkCore;
using Moq;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;

namespace MqttRestApi.Tests;

public partial class MqttServiceTests
{
    public static IEnumerable<object[]> TestPlanSubscribeTopicCases()
    {
        var valid = new[]
        {
            "a", "a/b", "house/room/temperature", "#", "+", "a/+", "a/#", "+/b", "a/+/c", "a/b/#",
            "device-1/state", "home/kitchen/humidity", "sensor_01/value", "a1/b2/c3", "x.y/z.w", "one/two/three/four",
            "+/+/+", "root/+/value", "root/room/#", "telemetry/device-7/temperature", "A/B/C", "0/1/2", "underscore_name/ok",
            "dashes-are-ok/here", "many/levels/are/allowed/too"
        };
        foreach (var topic in valid) yield return new object[] { topic, true };

        var invalid = new[]
        {
            "", " ", "a b", "/a", "a/", "a//b", "a/#/b", "a/+/b/#/c", "a+b", "a#", "a/#suffix",
            "a/##", "a/++", "a/+/+/#/b", "a\tb", "a\nb", "a/#/", "//", "///", "a b/c", "a/c d",
            "a/+/b/#/c/d", "a/#/c", "a/+suffix", "a/suffix+"
        };
        foreach (var topic in invalid) yield return new object[] { topic, false };
    }

    [Theory]
    [MemberData(nameof(TestPlanSubscribeTopicCases))]
    public async Task SubscribeTopic_ExtremeTopicSyntaxMatrix(string topic, bool expectedValid)
    {
        var f = CreateService();
        if (!expectedValid)
        {
            await Assert.ThrowsAsync<BadRequestException>(() => f.Service.SubscribeAsync(topic));
            Assert.Equal(0, await f.WithDb(db => db.Topics.CountAsync()));
            return;
        }

        await f.Service.SubscribeAsync(topic);
        Assert.True(await f.WithDb(db => db.Topics.AnyAsync(t => t.Name == topic)));
    }

    public static IEnumerable<object[]> TestPlanMqttPayloadCases()
    {
        var rows = new List<object[]>();
        var bothValues = new[] { "-100", "-1", "0", "1", "9.99", "10", "10.01", "15", "20", "29.99", "30", "30.01", "31", "100", "999", "NaN", "Infinity", "-Infinity", "not-a-number" };
        foreach (var payload in bothValues)
            rows.Add(new object[] { (int)AlarmTypes.Both, 10, 30, payload, ExpectedAlarm(AlarmTypes.Both, 10, 30, payload) });

        var maxValues = new[] { "-1", "0", "10", "29.99", "30", "30.01", "100", "NaN", "Infinity", "garbage" };
        foreach (var payload in maxValues)
            rows.Add(new object[] { (int)AlarmTypes.Maximum, 10, 30, payload, ExpectedAlarm(AlarmTypes.Maximum, 10, 30, payload) });

        var minValues = new[] { "-100", "0", "9.99", "10", "10.01", "30", "100", "not numeric", "NaN", "-Infinity" };
        foreach (var payload in minValues)
            rows.Add(new object[] { (int)AlarmTypes.Minimum, 10, 30, payload, ExpectedAlarm(AlarmTypes.Minimum, 10, 30, payload) });

        return rows;
    }

    private static ValueAlarmTypes ExpectedAlarm(AlarmTypes type, int lower, int upper, string payload)
    {
        if (!float.TryParse(payload, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var value))
            return ValueAlarmTypes.NONE;
        if ((type == AlarmTypes.Both || type == AlarmTypes.Maximum) && upper < value)
            return ValueAlarmTypes.ALARM_HIGH;
        if ((type == AlarmTypes.Both || type == AlarmTypes.Minimum) && lower > value)
            return ValueAlarmTypes.ALARM_LOW;
        return ValueAlarmTypes.NONE;
    }

    [Theory]
    [MemberData(nameof(TestPlanMqttPayloadCases))]
    public async Task IncomingMqttPayload_AlarmClassificationMatrix(int typeValue, int lower, int upper,
        string payload, ValueAlarmTypes expected)
    {
        var f = CreateService();
        const string topic = "qa/edge/value";
        await SeedTopicWithAlarm(f, topic, (AlarmTypes)typeValue, lower, upper);
        await RaiseMessage(f, topic, payload);
        var saved = await f.WithDb(db => db.Messages.SingleAsync());
        Assert.Equal(payload, saved.Payload);
        Assert.Equal(expected, saved.AlarmType);
        f.Redis.Verify(r => r.SaveMessageAsync(It.IsAny<MQTTRestApi.Domain.Models.MqttMessage>()), Times.Once);
    }
}
