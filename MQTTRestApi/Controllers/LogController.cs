using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Controllers;

[Authorize(Roles = "Admin"), ApiController, Route("api/logs")]
public class LogController : ControllerBase
{
    private ILoggerService loggerService;

    public LogController(ILoggerService loggerService)
    {
        this.loggerService = loggerService;
        
    }
    
    [HttpGet("all-logs")]
    public async Task<IActionResult> MyTopics()
    {
        var list = await loggerService.GetLogs();
        
        return Ok(list);
    }
}