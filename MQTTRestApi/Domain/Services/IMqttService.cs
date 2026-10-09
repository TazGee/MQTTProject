using MQTTRestApi.Domain.DTO;
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
    /// Pretplacuje na topic
    /// </summary>
    /// <param name="topic">Topic</param>
    /// <returns>True ako je uspesno ili false ako nije.</returns>
    Task<bool> SubscribeAsync();
    
    /// <summary>
    /// Lista poruka iz baze
    /// </summary>
    /// <returns>Vraca listu poruka iz baze podataka.</returns>
    List<MqttMessage> GetMessages();

    Task<bool> SubscribeUserAsync(SubscribeRequestDto request, int userId);
    Task<bool> UnsubscribeUserAsync(int topicId, int userId);
    Task<List<TopicListDto>> MyTopics(int userId);
}