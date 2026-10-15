namespace MQTTRestApi.Domain.DTO;

/// <summary>
/// Data transfer object za zahtev za pretplatu na topic
/// </summary>
public class SubscribeRequestDto
{
    public int TopicId { get; set; } = 0;
    public string Topic { get; set; } = String.Empty;
}