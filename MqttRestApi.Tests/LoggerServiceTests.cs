using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Services;

namespace MqttRestApi.Tests;

public class LoggerServiceTests
{
    [Fact]
    public async Task LogMessage_SavesLogWithMessageAndType()
    {
        var db = TestHelpers.CreateDb();
        var svc = new LoggerService(db);

        await svc.LogMessage("Test poruka", LogTypes.INFO);

        var log = await db.Logs.SingleAsync();
        Assert.Equal("Test poruka", log.Message);
        Assert.Equal(LogTypes.INFO, log.Type);
    }

    [Fact]
    public async Task GetLogs_ReturnsAllSavedLogs()
    {
        var db = TestHelpers.CreateDb();
        var svc = new LoggerService(db);
        await svc.LogMessage("prva", LogTypes.INFO);
        await svc.LogMessage("druga", LogTypes.ERROR);

        var logs = await svc.GetLogs();

        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message == "prva");
        Assert.Contains(logs, l => l.Message == "druga");
    }

    [Fact]
    public async Task GetLogs_NoLogs_ReturnsEmptyList()
    {
        var svc = new LoggerService(TestHelpers.CreateDb());

        Assert.Empty(await svc.GetLogs());
    }

    [Fact]
    public async Task LogMessage_DisposedContext_RethrowsException()
    {
        var db = TestHelpers.CreateDb();
        var svc = new LoggerService(db);
        await db.DisposeAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => svc.LogMessage("x", LogTypes.INFO));
    }
}