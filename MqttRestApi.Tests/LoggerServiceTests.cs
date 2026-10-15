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

        await svc.LogMessage("Test", LogTypes.INFO);

        var log = await db.Logs.SingleAsync();
        Assert.Equal("Test", log.Message);
        Assert.Equal(LogTypes.INFO, log.Type);
    }

    [Fact]
    public async Task GetLogs_ReturnsAllSavedLogs()
    {
        var db = TestHelpers.CreateDb();
        var service = new LoggerService(db);
        await service.LogMessage("test1", LogTypes.INFO);
        await service.LogMessage("test2", LogTypes.ERROR);

        var logs = await service.GetLogs();

        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.Message == "test1");
        Assert.Contains(logs, l => l.Message == "test2");
    }

    [Fact]
    public async Task GetLogs_NoLogs_ReturnsEmptyList()
    {
        var service = new LoggerService(TestHelpers.CreateDb());

        Assert.Empty(await service.GetLogs());
    }

    [Fact]
    public async Task LogMessage_DisposedContext_RethrowsException()
    {
        var db = TestHelpers.CreateDb();
        var service = new LoggerService(db);
        await db.DisposeAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => service.LogMessage("x", LogTypes.INFO));
    }
}