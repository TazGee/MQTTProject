namespace MQTTRestApi.Domain.DTO;

public class TopicListDto
{
    public int TopicId { get; set; }
    public string TopicName { get; set; }

    public TopicListDto(int TopicId, string TopicName)
    {
        this.TopicId = TopicId;
        this.TopicName = TopicName;
    }
}