using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Services;

namespace MQTTRestApi.Controllers;

[ApiController]
[Route("api/mqtt")]
public class MqttController : ControllerBase
{
    private MqttService mqttService;

    public MqttController(MqttService mqttService)
    {
        this.mqttService = mqttService;
    }
    
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromQuery] SubscribeRequestDto request)
    {
        var result = await mqttService.SubscribeAsync(request.Topic);

        if (!result)
            return BadRequest("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    
    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromQuery] PublishRequestDto request)
    {
        var result = await mqttService.PublishAsync(request.Topic, request.Payload, request.QoS);

        if (!result)
            return BadRequest("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    
    [HttpGet("messages")]
    public IActionResult GetMessages()
    {
        var messages = mqttService.GetMessages();
        return Ok(messages);
    }
}