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
        if (!await dbContext.Topics.AnyAsync(s => s.Id == createAlarmDto.TopicId)) throw new BadRequestException($"Topic sa id-em {createAlarmDto.TopicId} ne postoji!");
        if (await dbContext.Alarms.AnyAsync(a => a.TopicId == createAlarmDto.TopicId)) throw new BadRequestException($"Vec postoji alarm na topicu: {createAlarmDto.TopicId}!");
        if (createAlarmDto.AlarmType == AlarmTypes.Both && createAlarmDto.LowerLimit >= createAlarmDto.UpperLimit) throw new BadRequestException("Donja granica mora biti manja od gornje granice!");
        
        if(!Enum.IsDefined(typeof(AlarmTypes), createAlarmDto.AlarmType)) throw new BadRequestException("Neispravan tip alarma!");
        
        if(double.IsNaN(createAlarmDto.LowerLimit) && (createAlarmDto.AlarmType == AlarmTypes.Both || createAlarmDto.AlarmType == AlarmTypes.Minimum)) 
            throw new BadRequestException("Lower limit nije definisan!");
        if(double.IsNaN(createAlarmDto.UpperLimit) && (createAlarmDto.AlarmType == AlarmTypes.Both || createAlarmDto.AlarmType == AlarmTypes.Maximum)) 
            throw new BadRequestException("Upper limit nije definisan!");
        
        if(createAlarmDto.LowerLimit < double.MinValue) throw new BadRequestException("Lower limit nije ispravna vrednost!");
        if(createAlarmDto.UpperLimit > double.MaxValue) throw new BadRequestException("Upper limit nije ispravna vrednost!");
        
        Alarm alarm = new Alarm(createAlarmDto.TopicId, createAlarmDto.AlarmType, createAlarmDto.LowerLimit, createAlarmDto.UpperLimit);

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