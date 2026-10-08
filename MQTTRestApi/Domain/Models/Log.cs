using MQTTRestApi.Domain.Enums;

namespace MQTTRestApi.Domain.Models;

public class Log
{
    public int Id { get; set; }
    public string Message { get; set; }
    public LogTypes Type { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Log() {}
    public Log(string msg, LogTypes type)
    {
        Message = msg;
        Type = type;
        CreatedAt = DateTime.Now;
    }

    public override string ToString()
    {
        return $"[{CreatedAt:HH:mm:ss} - {Type.ToString()}] : {Message}";
    }
}