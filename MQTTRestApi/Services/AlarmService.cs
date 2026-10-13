using Microsoft.EntityFrameworkCore;
using MQTTRestApi.Data;
using MQTTRestApi.Domain.DTO;
using MQTTRestApi.Domain.Enums;
using MQTTRestApi.Domain.Exceptions;
using MQTTRestApi.Domain.Models;
using MQTTRestApi.Domain.Services;

namespace MQTTRestApi.Services;

public class AlarmService : IAlarmService
{
    private readonly AppDbContext dbContext;
    private readonly ILoggerService loggerService;
    
    public AlarmService(AppDbContext dbContext, ILoggerService loggerService)
    {
        this.dbContext = dbContext;
        this.loggerService = loggerService;
    }

    public async Task CreateAlarm(int topicId, AlarmTypes alarmType, int lowerLimit, int upperLimit)
    {
        if (!await dbContext.Topics.AnyAsync(s => s.Id == topicId)) throw new BadRequestException($"Topic sa id-em {topicId} ne postoji!");
        if (alarmType == AlarmTypes.Both && lowerLimit >= upperLimit) throw new BadRequestException("Donja granica mora biti manja od gornje granice!");
        
        Alarm alarm = new Alarm(topicId, alarmType, lowerLimit, upperLimit);

        try
        {
            await dbContext.Alarms.AddAsync(alarm);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            await loggerService.LogMessage($"Neuspesno dodavanje alarma u bazu. {e.Message}", LogTypes.ERROR);
            throw new BadRequestException($"Neuspesno dodavanje alarma u bazu. Probajte opet kasnije! {e.Message}");
        }
    }

    public async Task DeleteAlarm(int id)
    {
        var alarm = await dbContext.Alarms.FindAsync(id);
        if (alarm == null) throw new BadRequestException($"Alarm sa id-em {id} ne postoji!");

        try
        {
            dbContext.Alarms.Remove(alarm);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            await loggerService.LogMessage($"Neuspesno dodavanje alarma u bazu. {e.Message}", LogTypes.ERROR);
            throw new BadRequestException($"Neuspesno dodavanje alarma u bazu. Probajte opet kasnije! {e.Message}");
        }
    }

    public async Task<List<Alarm>> GetAllAlarms()
    {
        return await dbContext.Alarms.ToListAsync();
    }
    
    public async Task<List<MqttMessageDto>> GetAllAlarmedMessages()
    {
        var lista = await dbContext.Messages
            .Where(m => m.AlarmType != ValueAlarmTypes.NONE)
            .Select(m => new MqttMessageDto(m.Id, m.Topic, m.Payload, m.QoS, m.Retain, m.RecievedAt, m.AlarmType))
            .ToListAsync();
        return lista;
    }
    
}