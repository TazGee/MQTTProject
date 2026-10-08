using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

/// <summary>
/// Interfejs za MqttService
/// </summary>
public interface IMqttService
{
    /// <summary>
    /// Povezuje servis sa serverom
    /// </summary>
    Task ConnectAsync();
    
    /// <summary>
    /// Ponovno pretplacuje na topic-e
    /// </summary>
    Task ResubscribeToAll();
    
    /// <summary>
    /// Pretplacuje na topic
    /// </summary>
    /// <param name="topic">Topic</param>
    /// <returns>True ako je uspesno ili false ako nije.</returns>
    Task<bool> SubscribeAsync(string topic);
    
    /// <summary>
    /// Lista poruka iz baze
    /// </summary>
    /// <returns>Vraca listu poruka iz baze podataka.</returns>
    List<MqttMessage> GetMessages();
}