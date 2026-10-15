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

public class AlarmControllerTests
{
    private static (AlarmController controller, Mock<IAlarmService> service) Create()
    {
        var service = new Mock<IAlarmService>();
        return (new AlarmController(service.Object), service);
    }

    [Fact]
    public void Controller_RequiresAdminRole()
    {
        var attr = typeof(AlarmController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single();

        Assert.Equal("Admin", attr.Roles);
    }

    [Fact]
    public async Task CreateAlarm_CallsServiceAndReturnsOk()
    {
        var (controller, service) = Create();

        var result = await controller.CreateAlarm(new CreateAlarmDto(3, AlarmTypes.Both, 10, 30));

        service.Verify(s => s.CreateAlarm(It.Is<CreateAlarmDto>(d =>
            d.topicID == 3 && d.AlarmType == AlarmTypes.Both &&
            d.lowerLimit == 10 && d.upperLimit == 30)), Times.Once);
    }

    [Fact]
    public async Task DeleteAlarm_CallsServiceAndReturnsOk()
    {
        var (controller, service) = Create();

        var result = await controller.DeleteAlarm(5);

        service.Verify(s => s.DeleteAlarm(5), Times.Once);
        Assert.Equal("Uspesno obrisan alarm!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarms_Empty_ReturnsMessage()
    {
        var (controller, service) = Create();
        service.Setup(s => s.GetAllAlarms()).ReturnsAsync(new List<AlarmDto>());

        var result = await controller.GetAllAlarms();

        Assert.Equal("Lista alarma je prazna!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarms_NotEmpty_ReturnsList()
    {
        var (controller, service) = Create();
        var list = new List<AlarmDto> { new (1, 1, AlarmTypes.Both, 10, 30, DateTime.Now) };
        service.Setup(s => s.GetAllAlarms()).ReturnsAsync(list);

        var result = await controller.GetAllAlarms();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarmedMessages_Empty_ReturnsMessage()
    {
        var (controller, service) = Create();
        service.Setup(s => s.GetAllAlarmedMessages()).ReturnsAsync(new List<MqttMessageDto>());

        var result = await controller.GetAllAlarmedMessages();

        Assert.Equal("Lista poruka je prazna!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarmedMessages_NotEmpty_ReturnsList()
    {
        var (controller, service) = Create();
        var list = new List<MqttMessageDto>
        {
            new(1, "kuca/test", "35", MqttQualityOfServiceLevel.AtMostOnce, false, DateTime.Now, ValueAlarmTypes.ALARM_HIGH)
        };
        service.Setup(s => s.GetAllAlarmedMessages()).ReturnsAsync(list);

        var result = await controller.GetAllAlarmedMessages();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }
}