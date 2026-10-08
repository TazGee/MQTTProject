using MQTTDevice.Domain.Models;
using System;

namespace MQTTDevice;

class Program
{
    static Random random = new Random();
    
    private static int refreshTime = -1;
    private static string deviceName;
    private static int minValue, maxValue;
    
    private static List<Uredjaj> uredjaji = new List<Uredjaj>();
    
    static async Task Main(string[] args)
    {
        Console.Clear();
        Console.WriteLine($"00========--------------=======00");
        Console.WriteLine("- = == MQTT DEVICE SELECTOR == = -\n");

        ProcessInput();
        
        await RunLoop();
    }

    static void ProcessInput()
    {
        do
        {
            try
            {
                Console.Write("Unesite topic uredjaja: ");
                deviceName = Console.ReadLine();
            }
            catch { }
        } while (String.IsNullOrEmpty(deviceName));
        Console.Clear();
        
        do
        {
            try
            {
                Console.Write("Unesite (u sekundama) vreme osvezavanja (min 1, max 10): ");
                refreshTime = int.Parse(Console.ReadLine());
            }
            catch { }
        } while (refreshTime == -1 || refreshTime < 1 || refreshTime > 10);
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
            Console.WriteLine($"{deviceName}: {value}");
            
            await Task.Delay(TimeSpan.FromSeconds(refreshTime));
        }
    }
}