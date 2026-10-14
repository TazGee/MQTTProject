using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MQTTRestApi.Controllers;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MqttRestApi.Tests;

public class LogControllerTests
{
    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attr = typeof(LogController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();

        Assert.Equal("Admin", attr.Roles);
    }

    [Fact]
    public async Task GetAllLogs_ReturnsOkWithLogsFromService()
    {
        var svc = new Mock<ILoggerService>();
        var logs = new List<Log> { new("poruka", LogTypes.INFO) };
        svc.Setup(s => s.GetLogs()).ReturnsAsync(logs);
        var ctrl = new LogController(svc.Object);

        var result = await ctrl.MyTopics();

        Assert.Same(logs, Assert.IsType<OkObjectResult>(result).Value);
    }
}