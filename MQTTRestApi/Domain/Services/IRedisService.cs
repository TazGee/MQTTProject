using MQTTRestApi.Domain.Models;

namespace MQTTRestApi.Domain.Services;

/// <summary>
/// Interfejs za RedistService
/// </summary>
public interface IRedisService
{
    /// <summary>
    /// Salje poruku na redis
    /// </summary>
    /// <param name="messasge">Sadrzaj poruke</param
    Task SaveMessageAsync(MqttMessage message);
    
    /// <summary>
    /// Povecava counter za topic poruke
    /// </summary>
    /// <param name="topic">Topic poruke</param>
    Task IncrementTopicCountAsync(string topic);
    
    /// <summary>
    /// Vraca broj poruka za svaki topic
    /// </summary>
    /// <returns>Dictionary: topic - broj poruka</returns>
    Task<Dictionary<string, int>> GetAllTopicsCountAsync();

    /// <summary>
    /// Vraca broj poruka za neki topic
    /// </summary>
    /// <param name="topic">Topic poruke</param>
    /// <returns>Broj poruka za neki topic</returns>
    Task<int> GetTopicCountAsync(string topic);
    
    Task AddAsync(string topic, int userId);
    Task RemoveAsync(string topic, int userId);
    Task<bool> IsSubscribedAsync(string topic, int userId);
    Task<IReadOnlyList<string>> GetUserTopicsAsync(int userId);
    Task LoadAllAsync();
}