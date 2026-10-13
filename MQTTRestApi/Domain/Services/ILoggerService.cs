using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

public interface ILoggerService
{
    Task<List<Log>> GetLogs();
    Task LogMessage(string message, LogTypes type);
}