using Microsoft.AspNetCore.Mvc;
using MQTTnet.Protocol;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Services;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MQTTRestApi.Domain.Models;
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

    [HttpGet("subscribe"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Subscribe(string topic)
    {
        var result = await mqttService.SubscribeAsync(topic);
        
        if (!result) return Forbid("Neuspesan pokusaj subscribe-a.");
        
        return Ok();
    }
    
    [HttpPost("user-subscribe")]
    public async Task<IActionResult> UserSubscribe(SubscribeRequestDto request)
    {
        var result = await mqttService.SubscribeUserAsync(request, UserId());

        if (!result) return Forbid("Neuspesan pokusaj subscribe-a.");

        return Ok();
    }
    
    [HttpDelete("user-unsubscribe/{*topicId}")]
    public async Task<IActionResult> UserUnsubscribe(int topicId)
    {
        var result = await mqttService.UnsubscribeUserAsync(topicId, UserId());

        if (!result) return BadRequest("Neuspesan pokusaj unsubscribe-a.");

        return Ok();
    }

    [HttpGet("my-subscriptions")]
    public async Task<IActionResult> MyTopics()
    {
        var list = await mqttService.MyTopics(UserId());
        
        if(list.Count == 0) return BadRequest();
        
        return Ok(list);
    }
    
    [HttpGet("failed-subscriptions"), Authorize(Roles = "Admin")]
    public IActionResult FailedSubscriptions()
    {
        var list = mqttService.GetFailedTopics();
        
        if(list.Count == 0) return BadRequest();
        
        return Ok(list);
    }
    
    [HttpGet("resubscribe-to-failed"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResubscribeToFailed()
    {
        var lista = await mqttService.ResubscribeToFailed();
        
        if(lista.Count ==  0) return BadRequest();
        
        return Ok(lista);
    }
    
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await mqttService.GetMessages(UserId());
        
        if (messages.Count == 0) return BadRequest();
        
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
    
    [HttpGet("force-reconnect-to-server"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReconnectToServer()
    {
        if(await mqttService.ForceReconnect()) return Ok();
        else return BadRequest();
    }
}