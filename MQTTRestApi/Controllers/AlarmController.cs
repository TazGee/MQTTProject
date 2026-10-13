using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Controllers;

[Authorize(Roles = "Admin"), ApiController, Route("api/alarms")]
public class AlarmController : ControllerBase
{
    private IAlarmService alarmService;

    public AlarmController(IAlarmService alarmService)
    {
        this.alarmService = alarmService;
    }
    
    [HttpPost("create")]
    public async Task<IActionResult> CreateAlarm(CreateAlarmDto createAlarmDto)
    {
        await alarmService.CreateAlarm(createAlarmDto);
        
        return Ok("Uspesno kreiran alarm!");
    }
    
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteAlarm(int id)
    {
        await alarmService.DeleteAlarm(id);
        
        return Ok("Uspesno obrisan alarm!");
    }
    
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAlarms()
    {
        var alarms = await alarmService.GetAllAlarms();
        
        if(alarms.Count == 0) return Ok("Lista alarma je prazna!");
        
        return Ok(alarms);
    }
    
    [HttpGet("alarmed-messages")]
    public async Task<IActionResult> GetAllAlarmedMessages()
    {
        var messages = await alarmService.GetAllAlarmedMessages();
        
        if(messages.Count == 0) return Ok("Lista poruka je prazna!");
        
        return Ok(messages);
    }
}