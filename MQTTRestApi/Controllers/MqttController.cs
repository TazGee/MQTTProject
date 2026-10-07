using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Services;
using System.Net;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Controllers;

/// <summary>
/// Controller sa endpointima
/// </summary>
[ApiController]
[Route("api/mqtt")]
public class MqttController : ControllerBase
{
    private IMqttService mqttService;
    private IRedisService redisService;

    public MqttController(IMqttService mqttService, IRedisService redisService)
    {
        this.mqttService = mqttService;
        this.redisService = redisService;
    }
    
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromQuery] SubscribeRequestDto request)
    {
        var result = await mqttService.SubscribeAsync(request.Topic);

        if (!result) return BadRequest("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    
    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromQuery] PublishRequestDto request)
    {
        var result = await mqttService.PublishAsync(request.Topic, request.Payload, request.QoS);

        if (!result) return BadRequest("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    
    [HttpGet("messages")]
    public IActionResult GetMessages()
    {
        var messages = mqttService.GetMessages();
        return Ok(messages);
    }
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var allTopics = await redisService.GetAllTopicsCountAsync();
        
        return Ok(allTopics);
    }
    
    [HttpGet("stats/{*topic}")]
    public async Task<IActionResult> GetTopicStats(string topic)
    {
        topic = WebUtility.UrlDecode(topic);
        
        var count = await redisService.GetTopicCountAsync(topic);

        return Ok(new
        {
            Topic = topic,
            Count = count
        });
    }
}