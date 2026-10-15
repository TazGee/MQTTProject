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
        var service = new Mock<ILoggerService>();
        var logs = new List<Log> { new("poruka", LogTypes.INFO) };
        service.Setup(s => s.GetLogs()).ReturnsAsync(logs);
        var controller = new LogController(service.Object);

        var result = await controller.MyTopics();

        Assert.Same(logs, Assert.IsType<OkObjectResult>(result).Value);
    }
}