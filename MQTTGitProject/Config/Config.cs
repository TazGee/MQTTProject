using System.Text.Json;

namespace MQTTGitProject.Config;

/// <summary>
/// Klasa u koju se deserijalizuju podaci iz JSON-a
/// </summary>
public class Config
{
    public string SubscribeRegex { get; set; }
    public string PublishRegex { get; set; }
    
    public string LogFilePath { get; set; }
    
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }
    public int ReconnectTimer { get; set; }

    public static Config Load()
    {
        string json = File.ReadAllText("config.json");

        return JsonSerializer.Deserialize<Config>(json);
    }
    
    bool DeserializeConfig()
    {
        try
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            
            SubscribeRegex =  config.SubscribeRegex;
            PublishRegex =  config.PublishRegex;
            
            LogFilePath =  config.LogFilePath;
            
            ServerIP =  config.ServerIP;
            ServerPort =  config.ServerPort;
            
            ReconnectTimer = config.ReconnectTimer;
            
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}