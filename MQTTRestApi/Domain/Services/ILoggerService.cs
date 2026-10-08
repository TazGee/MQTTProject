using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Services;

public interface ILoggerService
{
    Task LogMessage(string message, LogTypes type);
}