using System.Text.Json;

namespace MQTTGitProject.Config;

/// <summary>
/// Klasa u koju se deserijalizuju podaci iz JSON-a
/// </summary>
public class Config
{
    public string PublishRegex { get; set; }
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }
    public uint ReconnectTimer { get; set; }

    public static Config Load()
    {
        string json = File.ReadAllText("config.json");

        return JsonSerializer.Deserialize<Config>(json);
    }
}