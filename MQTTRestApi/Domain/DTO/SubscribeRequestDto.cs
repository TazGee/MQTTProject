namespace MQTTRestApi.Domain.DTO;

/// <summary>
/// Data transfer object za zahtev za pretplatu na topic
/// </summary>
public class SubscribeRequestDto
{
    public string Topic { get; set; }
}