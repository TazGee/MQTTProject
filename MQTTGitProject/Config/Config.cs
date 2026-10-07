using System.Text.Json;

namespace MQTTGitProject.Config;

public class Config
{
    public string SubscribeRegex { get; set; }
    public string PublishRegex { get; set; }
    
    public string LogFilePath { get; set; }
    
    public string ServerIP { get; set; }
    public int ServerPort { get; set; }

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
            
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }
}