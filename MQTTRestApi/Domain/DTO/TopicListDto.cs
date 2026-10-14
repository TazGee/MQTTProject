namespace MQTTRestApi.Domain.DTO;

public class TopicListDto
{
    public int TopicId { get; set; }
    public string TopicName { get; set; }

    public TopicListDto(int topicId, string topicName)
    {
        TopicId = topicId;
        TopicName = topicName;
    }
}