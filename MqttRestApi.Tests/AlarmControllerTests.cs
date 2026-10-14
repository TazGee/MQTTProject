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
    private static (AlarmController ctrl, Mock<IAlarmService> svc) Create()
    {
        var svc = new Mock<IAlarmService>();
        return (new AlarmController(svc.Object), svc);
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
        var (ctrl, svc) = Create();

        var result = await ctrl.CreateAlarm(new CreateAlarmDto(3, AlarmTypes.Both, 10, 30));

        svc.Verify(s => s.CreateAlarm(It.Is<CreateAlarmDto>(d =>
            d.topicID == 3 && d.AlarmType == AlarmTypes.Both &&
            d.lowerLimit == 10 && d.upperLimit == 30)), Times.Once);
    }

    [Fact]
    public async Task DeleteAlarm_CallsServiceAndReturnsOk()
    {
        var (ctrl, svc) = Create();

        var result = await ctrl.DeleteAlarm(5);

        svc.Verify(s => s.DeleteAlarm(5), Times.Once);
        Assert.Equal("Uspesno obrisan alarm!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarms_Empty_ReturnsMessage()
    {
        var (ctrl, svc) = Create();
        svc.Setup(s => s.GetAllAlarms()).ReturnsAsync(new List<AlarmDto>());

        var result = await ctrl.GetAllAlarms();

        Assert.Equal("Lista alarma je prazna!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarms_NotEmpty_ReturnsList()
    {
        var (ctrl, svc) = Create();
        var list = new List<AlarmDto> { new (1, 1, AlarmTypes.Both, 10, 30, DateTime.Now) };
        svc.Setup(s => s.GetAllAlarms()).ReturnsAsync(list);

        var result = await ctrl.GetAllAlarms();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarmedMessages_Empty_ReturnsMessage()
    {
        var (ctrl, svc) = Create();
        svc.Setup(s => s.GetAllAlarmedMessages()).ReturnsAsync(new List<MqttMessageDto>());

        var result = await ctrl.GetAllAlarmedMessages();

        Assert.Equal("Lista poruka je prazna!", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetAllAlarmedMessages_NotEmpty_ReturnsList()
    {
        var (ctrl, svc) = Create();
        var list = new List<MqttMessageDto>
        {
            new(1, "kuca/test", "35", MqttQualityOfServiceLevel.AtMostOnce, false, DateTime.Now, ValueAlarmTypes.ALARM_HIGH)
        };
        svc.Setup(s => s.GetAllAlarmedMessages()).ReturnsAsync(list);

        var result = await ctrl.GetAllAlarmedMessages();

        Assert.Same(list, Assert.IsType<OkObjectResult>(result).Value);
    }
}