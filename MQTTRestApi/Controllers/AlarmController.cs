using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Controllers;

[Authorize(Roles = "Admin"), ApiController, Route("api/alarms")]
public class AlarmController : ControllerBase
{
    [HttpPost("create-alarm")]
    public async Task<IActionResult> CreateAlarm(int topicId, AlarmTypes alarmType,  int lowerLimit, int upperLimit)
    {
        
        
        return Ok();
    }
}