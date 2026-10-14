using System.Text.Json;

namespace MQTTDevice.Config;

/// <summary>
/// Klasa u koju se deserijalizuju podaci iz JSON-a
/// </summary>
public class Config
{
    public string PublishRegex { get; set; }
    public string ServerIp { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 1883;
    public uint ReconnectTimer { get; set; } = 3;
    public float VrednostSimulatora { get; set; } = 20f;

    public static Config Load()
    {
        string json = File.ReadAllText("config.json");

        return JsonSerializer.Deserialize<Config>(json) ??  new Config();
    }
}