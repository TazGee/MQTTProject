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

    [HttpPost("subscribe"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
    {
        await mqttService.SubscribeAsync(request.Topic);
        
        return Ok("Subscribe uspesan!");
    }
    
    [HttpPost("user-subscribe")]
    public async Task<IActionResult> UserSubscribe(SubscribeRequestDto request)
    {
        await mqttService.SubscribeUserAsync(request, UserId());

        return Ok("User subscribe uspesan!");
    }
    
    [HttpDelete("user-unsubscribe/{*topicId}")]
    public async Task<IActionResult> UserUnsubscribe(int topicId)
    {
        await mqttService.UnsubscribeUserAsync(topicId, UserId());

        return Ok("User unsubscribe uspesan!");
    }

    [HttpGet("my-subscriptions")]
    public async Task<IActionResult> MyTopics()
    {
        var list = await mqttService.MyTopics(UserId());
        
        if(list.Count == 0) return Ok("Lista vasih topic-a je prazna.");
        
        return Ok(list);
    }
    
    [HttpGet("failed-subscriptions"), Authorize(Roles = "Admin")]
    public IActionResult FailedSubscriptions()
    {
        var list = mqttService.GetFailedTopics();
        
        return Ok(list);
    }
    
    [HttpPost("resubscribe-to-failed"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResubscribeToFailed()
    {
        var lista = await mqttService.ResubscribeToFailed();
        
        if(lista.Count == 0) return Ok("Lista uspesno resubscribed je prazna.");
        
        return Ok(lista);
    }
    
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var messages = await mqttService.GetMessages(UserId());
        
        if(messages.Count == 0) return Ok("Nemate dostupnih poruka.");
        
        return Ok(messages);
    }
    
    [Authorize(Roles = "Admin"), HttpGet("all-messages")]
    public async Task<IActionResult> GetAllMessages()
    {
        var messages = await mqttService.GetAllMessages();
        
        return Ok(messages);
    }
    
    [HttpGet("available-topics")]
    public async Task<IActionResult> GetAvailableTopics()
    {
        var topics = await mqttService.GetAvailableTopics();

        if (topics.Count == 0) return Ok("Nema dostupnih topic-a");
        
        return Ok(topics);
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
    
    [HttpPost("force-reconnect-to-server"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReconnectToServer()
    {
        await mqttService.ForceReconnect();
        
        return Ok("Force reconnect uspesan!");
    }
}