namespace MQTTDevice.Domain.Models;

public class Uredjaj
{
    public string Name { get; set; } =  string.Empty;
    public string UredjajTopic { get; set; } = string.Empty;
    public int maxValue { get; set; } = 0;
    public int minValue { get; set; } = 0;

    public override string ToString()
    {
        return  $"Naziv: {Name}\nTopic: {UredjajTopic}";
    }
}