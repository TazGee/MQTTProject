using MQTTRestApi.Data;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

public class LoggerService : ILoggerService
{
    private readonly AppDbContext dbContext;
    
    public LoggerService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task LogMessage(string msg, LogTypes type)
    {
        try
        {
            Log log = new Log(msg, type);
            Console.WriteLine(log.ToString());
            
            dbContext.Logs.Add(log);
            await dbContext.SaveChangesAsync();
            
            Console.WriteLine("Log uspesno sacuvan u bazi podataka");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Greska prilikom logging-a poruke: {e.Message}");
            throw;
        }
    }
}