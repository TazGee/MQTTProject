using MQTTDevice.Domain.Models;
using System;
using MQTTDevice.Domain.Services;
using MQTTDevice.Services;
using MQTTGitProject.Config;

namespace MQTTDevice;

class Program
{
    static Random random = new Random();
    
    private static int refreshTime = -1;
    private static string topic;
    private static int minValue, maxValue;

    private static IMqttService service = new MqttService();
    
    static async Task Main(string[] args)
    {
        Config config = Config.Load();
        Console.WriteLine(config.ServerIP +  ":" + config.ServerPort);
        
        Console.WriteLine($"00========--------------=======00");
        Console.WriteLine("- = == MQTT DEVICE SELECTOR == = -\n");

        ProcessInput();
        await service.InitializeService(config);
        
        await RunLoop();
    }

    static void ProcessInput()
    {
        do
        {
            try
            {
                Console.Write("Unesite topic uredjaja: ");
                topic = Console.ReadLine();
            }
            catch { }
        } while (String.IsNullOrEmpty(topic));
        Console.Clear();
        
        do
        {
            try
            {
                Console.Write("Unesite (u sekundama) vreme osvezavanja (min 2, max 10): ");
                refreshTime = int.Parse(Console.ReadLine());
            }
            catch { }
        } while (refreshTime < 2 || refreshTime > 10);
        Console.Clear();
        
        bool uspeo = false;
        do
        {
            try
            {
                Console.Write("Unesite minimalnu vrednost: ");
                minValue = int.Parse(Console.ReadLine());
                uspeo = true;
            }
            catch { }
        } while (!uspeo);
            
        uspeo = false;
        do
        {
            try
            {
                Console.Write("Unesite maksimalnu vrednost: ");
                maxValue = int.Parse(Console.ReadLine());
                uspeo = true;
            }
            catch { }
        } while (!uspeo);
        Console.Clear();
    }
    
    static async Task RunLoop()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine($"Refresh - Nova vrednost - {DateTime.Now:HH:mm:ss}");

            float value = (float)minValue + (float)(random.NextDouble() * (maxValue - minValue));
            Console.WriteLine($"{topic}: {value}");

            await service.PublishMessageAsync(topic, value.ToString());
            
            await Task.Delay(TimeSpan.FromSeconds(refreshTime));
        }
    }
}