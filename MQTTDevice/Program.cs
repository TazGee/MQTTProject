using System.Text.RegularExpressions;
using MQTTDevice.Domain.Services;
using MQTTDevice.Services;
using MQTTGitProject.Config;

namespace MQTTDevice;

class Program
{
    static Random random = new Random();
    
    private static int refreshTime = -1;
    private static string topic;
    private static int izbor = -1;
    private static float vrednostZaSlanje;

    private static IMqttService service = new MqttService();
    
    static async Task Main(string[] args)
    {
        Config config = Config.Load();
        Console.WriteLine(config.ServerIP +  ":" + config.ServerPort);
        
        Console.WriteLine($"00========--------------=======00");
        Console.WriteLine("- = == MQTT DEVICE SELECTOR == = -\n");

        ProcessInput(config);

        service.DeviceValueTopic = topic + "/updateValue";
        
        await service.InitializeService(config);

        service.VrednostSimulatora = config.VrednostSimulatora;
        
        await RunLoop(config);
    }

    static void ProcessInput(Config config)
    {
        do
        {
            try
            {
                Console.Write("Unesite topic uredjaja: ");
                topic = Console.ReadLine();
                if (!Regex.IsMatch(topic, config.PublishRegex))
                {
                    Console.WriteLine("Neispravan format topic-a! Mora biti u formatu rec(/rec...)");
                    Console.WriteLine("Primer: kuca/soba/temperatura");
                    topic = null!;
                }
            }
            catch { }
        } while (String.IsNullOrEmpty(topic));
        
        Console.WriteLine("Topic na koji ce vam admin slati azuriranja vrednosti:");
        Console.WriteLine(topic);
        
        
        do
        {
            try
            {
                Console.Write("Unesite (u sekundama) vreme osvezavanja (min 2, max 60): ");
                refreshTime = int.Parse(Console.ReadLine());
            }
            catch { }
        } while (refreshTime < 2 || refreshTime > 60);
    }
    
    static async Task RunLoop(Config config)
    {
        while (true)
        {
            Console.WriteLine($"Refresh - Nova vrednost - {DateTime.Now:HH:mm:ss}");

            vrednostZaSlanje = service.VrednostSimulatora;
            Console.WriteLine($"{topic}: {vrednostZaSlanje}");

            await service.PublishMessageAsync(topic, vrednostZaSlanje.ToString());
            await Task.Delay(TimeSpan.FromSeconds(refreshTime));
        }
    }
}