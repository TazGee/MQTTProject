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

    public async Task CreateAlarm(CreateAlarmDto createAlarmDto)
    {
        if (!await dbContext.Topics.AnyAsync(s => s.Id == createAlarmDto.topicID)) throw new BadRequestException($"Topic sa id-em {createAlarmDto.topicID} ne postoji!");
        if (!await dbContext.Alarms.AnyAsync(a => a.TopicId == createAlarmDto.topicID)) throw new BadRequestException($"Vec postoji alarm na topicu: {createAlarmDto.topicID}!");
        if (createAlarmDto.AlarmType == AlarmTypes.Both && createAlarmDto.lowerLimit >= createAlarmDto.upperLimit) throw new BadRequestException("Donja granica mora biti manja od gornje granice!");
        
        Alarm alarm = new Alarm(createAlarmDto.topicID, createAlarmDto.AlarmType, createAlarmDto.lowerLimit, createAlarmDto.upperLimit);

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
            await loggerService.LogMessage($"Neuspesno brisanje alarma iz baze. {e.Message}", LogTypes.ERROR);
            throw new BadRequestException($"Neuspesno brisanje alarma iz baze. Probajte opet kasnije! {e.Message}");
        }
    }

    public async Task<List<AlarmDto>> GetAllAlarms()
    {
        var lista =  await dbContext.Alarms
            .Select(a => new AlarmDto(a.Id, a.TopicId, a.AlarmType, a.LowerLimit, a.UpperLimit, a.CreatedAt))
            .ToListAsync();
        return lista;
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