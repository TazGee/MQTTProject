using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.DTO;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using MQTTRestApi.Domain.Services;
using System.Security.Claims;

namespace MQTTRestApi.Controllers;

/// <summary>
/// Controller sa endpointima
/// </summary>
[Authorize, ApiController, Route("api/mqtt")]
public class MqttController : ControllerBase
{
    private IMqttService mqttService;
    private IRedisService redisService;

    public MqttController(IMqttService mqttService, IRedisService redisService)
    {
        this.mqttService = mqttService;
        this.redisService = redisService;
        
    }

    private int UserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe(string topic)
    {
        var result = await mqttService.SubscribeAsync(topic);
        
        if (!result) return BadRequest("Neuspesan pokusaj subscribe-a.");
        
        return Ok();
    }
    
    [HttpPost("usersubscribe")]
    public async Task<IActionResult> UserSubscribe(SubscribeRequestDto request)
    {
        var result = await mqttService.SubscribeUserAsync(request, UserId());

        if (!result) return BadRequest("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    

    [HttpGet("mysubscriptions")]
    public async Task<IActionResult> MyTopics()
    {
        return Ok(await mqttService.MyTopics(UserId()));
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