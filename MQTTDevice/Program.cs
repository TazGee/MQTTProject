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
    private static int minValue, maxValue;
    private static bool nasumicno;
    private static int izbor;
    private static float vrednostZaSlanje;

    private static IMqttService service = new MqttService();
    
    static async Task Main(string[] args)
    {
        Config config = Config.Load();
        Console.WriteLine(config.ServerIP +  ":" + config.ServerPort);
        
        Console.WriteLine($"00========--------------=======00");
        Console.WriteLine("- = == MQTT DEVICE SELECTOR == = -\n");

        ProcessInput(config);
        await service.InitializeService(config);
        await service.SubscribeAsync(topic + "/updateValue");

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
        Console.WriteLine(topic + "/updateValue");
        
        
        do
        {
            try
            {
                Console.Write("Unesite (u sekundama) vreme osvezavanja (min 2, max 60): ");
                refreshTime = int.Parse(Console.ReadLine());
            }
            catch { }
        } while (refreshTime < 2 || refreshTime > 60);

        do
        {
            try
            {
                Console.Write("Unesite 1 ako zelite vrednost iz konfiguracije ili 0 ako zelite random vrednosti: ");
                izbor = int.Parse(Console.ReadLine());
                if(izbor == 0) nasumicno = true; else  nasumicno = false;
            }
            catch { }
        } while (izbor < 0 || izbor > 1);
        if (!nasumicno) return;
        
        do
        {
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
        } while (minValue >= maxValue);
    }
    
    static async Task RunLoop(Config config)
    {
        while (true)
        {
            Console.WriteLine($"Refresh - Nova vrednost - {DateTime.Now:HH:mm:ss}");

            if (nasumicno)
            {
                vrednostZaSlanje = (float)minValue + (float)(random.NextDouble() * (maxValue - minValue));
                Console.WriteLine($"{topic}: {vrednostZaSlanje}");
            }
            else
            {
                vrednostZaSlanje = service.VrednostSimulatora;
                Console.WriteLine($"{topic}: {vrednostZaSlanje}");
            }

            await service.PublishMessageAsync(topic, vrednostZaSlanje.ToString());
            await Task.Delay(TimeSpan.FromSeconds(refreshTime));
        }
    }
}