using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Handlers;

public class GlobalExceptionHandler(IServiceScopeFactory scopeFactory, IHostEnvironment env) : IExceptionHandler
{
    private async Task LogAsync(string message,  LogTypes logType)
    {
        using var scope = scopeFactory.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerService>();
        await logger.LogMessage(message, logType);
    }
    
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            AppException app => (app.ErrorCode, app.Message),
            _                => (StatusCodes.Status500InternalServerError, "Došlo je do greške na serveru.")
        };

        if (status >= 500) LogAsync($"Neobrađen izuzetak na {ex} ({ctx.Request.Path})", LogTypes.ERROR);
        else               LogAsync($"{ex.GetType()}: {ex.Message}", LogTypes.WARNING);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = ctx.Request.Path,
            
            Detail = env.IsDevelopment() && status >= 500 ? ex.ToString() : null
        };
        problem.Extensions["traceId"] = ctx.TraceIdentifier;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}