using System.Text.RegularExpressions;
using MQTTGitProject.Models;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTGitProject
{
    internal class Program 
    {
        static async Task Main(string[] args)
        {
            Config.Config config = Config.Config.Load();
            IMessageLogger testLogger = new CompositeLogger(new FileLogger(config), new ConsoleLogger());
            
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();

            List<Subscription> subscriptions = new List<Subscription>();
            
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(config.ServerIP, config.ServerPort)
                .Build();

            InitializeHandlers(testLogger, mqttClient, options, subscriptions);
            
            await TryToConnect(mqttClient, options);

            await MainLoop(config, mqttClient, subscriptions);
        }

        static async Task MainLoop(Config.Config config, IMqttClient mqttClient, List<Subscription> subscriptions)
        {
            string topic = String.Empty;
            int qos = 0;
            MqttQualityOfServiceLevel level = MqttQualityOfServiceLevel.AtMostOnce;
            
            while (true)
            {
                Console.WriteLine("Izaberite opciju: p - publish | s - subscribe | q - quit | c - clear");
                string opcija = Console.ReadLine();

                if (opcija == "p")
                {
                    string payload;
                    int tmpretain;
                    bool retain;
                
                    Console.Write("\nUnesite topic poruke: ");
                    topic = Console.ReadLine();
                    if (!Regex.IsMatch(topic, config.PublishRegex))
                    {
                        Console.WriteLine("Neispravan format topic-a! Mora biti u formatu rec(/rec...)");
                        Console.WriteLine("Primer: kuca/soba/temperatura");
                        continue;
                    }
                        
                    Console.Write("Unesite payload poruke: ");
                    payload = Console.ReadLine();
                
                    try
                    {
                        Console.WriteLine("1 - At Most Once\n2 - At Least Once\n3 - Exactly Once");
                        Console.Write("Unesite QoS: ");
                        qos =  int.Parse(Console.ReadLine());
                        
                        Console.WriteLine("1 - Retain\n2 - No Retain");
                        Console.Write("Unesite retain: ");
                        tmpretain =  int.Parse(Console.ReadLine());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Greska! Poruka greske: {e.Message}");
                        continue;
                    }
                    
                    switch (qos)
                    {
                        case 1:
                            level  = MqttQualityOfServiceLevel.AtMostOnce;
                            break;
                        case 2:
                            level  = MqttQualityOfServiceLevel.AtLeastOnce;
                            break;
                        case 3:
                            level  = MqttQualityOfServiceLevel.ExactlyOnce;
                            break;
                        default:
                            Console.WriteLine("Neispravan unos QoS!");
                            continue;
                    }
                    
                    switch (tmpretain)
                    {
                        case 1:
                            retain = true;
                            break;
                        case 2:
                            retain = false;
                            break;
                        default:
                            Console.WriteLine("Neispravan unos retain-a!");
                            continue;
                    }

                    try
                    {
                        await SendMessage(mqttClient, topic, payload, level, retain);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Greska pri slanju poruke: {e.Message}");
                        continue;
                    }
                }
                else if (opcija == "s")
                {
                    Console.Write("Unesite topic: ");
                    topic = Console.ReadLine();
                    if (!Regex.IsMatch(topic, config.SubscribeRegex))
                    {
                        Console.WriteLine("Neispravan format topic-a! Mora biti u formatu rec(/(rec/+/#)...)");
                        Console.WriteLine("Primer: kuca/soba/# ili kuca/+/# ili kuca/soba/temperatura...");
                        continue;
                    }
                    
                    Console.WriteLine("1 - At Most Once\n2 - At Least Once\n3 - Exactly Once");
                    Console.Write("Unesite QoS: ");
                    try
                    {
                        qos =  int.Parse(Console.ReadLine());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Greska! Poruka greske: {e.Message}");
                        continue;
                    }

                    switch (qos)
                    {
                        case 1:
                            level  = MqttQualityOfServiceLevel.AtMostOnce;
                            break;
                        case 2:
                            level  = MqttQualityOfServiceLevel.AtLeastOnce;
                            break;
                        case 3:
                            level  = MqttQualityOfServiceLevel.ExactlyOnce;
                            break;
                        default:
                            Console.WriteLine("Neispravan unos QoS!");
                            continue;
                    }
                    
                    try
                    {
                        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(topic, level)
                            .Build();
                        await mqttClient.SubscribeAsync(subscribeOptions);
                        
                        subscriptions.Add(new Subscription(topic, level));
                        Console.WriteLine($"Subscribe uspesno obavljen.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Doslo je do greske prilikom subscribe-a: {e.Message}");
                    }
                }
                else if (opcija == "q")
                {
                    Console.WriteLine("Izabrali ste opciju da napustite aplikaciju.");
                    break;
                }
                else if (opcija == "c")
                {
                    Console.Clear();
                }
                else
                {
                    Console.WriteLine("Uneli ste nepostojecu opciju!");
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
        
        static async Task TryToConnect(IMqttClient client, MqttClientOptions options)
        {
            do
            {
                try
                {
                    await client.ConnectAsync(options);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\nDoslo je do greske prilikom povezivanja na server: {e.Message}");
                    Console.WriteLine("Ponovni pokusaj za 5 sekundi...");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            } while (!client.IsConnected);
            
            Console.WriteLine("\nPovezivanje uspesno!");
        }
        
        static void InitializeHandlers(IMessageLogger logger, IMqttClient client, MqttClientOptions options, List<Subscription> subscriptions)
        {
            try
            {
                client.ApplicationMessageReceivedAsync += async e => 
                {
                    await logger.LogMessageAsync(e.ApplicationMessage.Topic, 
                        e.ApplicationMessage.ConvertPayloadToString(), 
                        e.ApplicationMessage.QualityOfServiceLevel.ToString(), 
                        (e.ApplicationMessage.Retain ? "Retain" : "No Retain"));
                };
            
                client.DisconnectedAsync += async e =>
                {
                    Console.WriteLine();
                    Console.WriteLine("MQTT konekcija je prekinuta");

                    if (e.Exception != null)
                    {
                        Console.WriteLine($"Razlog: {e.Exception.Message}");
                    }

                    while (!client.IsConnected)
                    {
                        try
                        {
                            Console.WriteLine("Pokusaj ponovnog povezivanja za 5 sekundi...");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                            
                            Console.WriteLine("Pokusavam reconnect...");

                            await client.ConnectAsync(options);

                            Console.WriteLine("Reconnect uspesan!");

                            await ResubscribeToAll(subscriptions, client);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Reconnect nije uspeo: {ex.Message}");
                        }
                    }
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"Greska pri inicijalizaciji handlera: {e.Message}");
            }
            
        }

        public static async Task ResubscribeToAll(List<Subscription> subscriptions, IMqttClient client)
        {
            for (int i = subscriptions.Count - 1; i >= 0; i--)
            {
                try
                {
                    var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(subscriptions[i].Topic, subscriptions[i].QoS)
                        .Build();
                    await client.SubscribeAsync(subscribeOptions);
                    
                    Console.WriteLine($"Uspesno re-subscribovan na {subscriptions[i].Topic}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Nije moguce ponovo povezati se na {subscriptions[i].Topic}");
                    Console.WriteLine($"Razlog: {e.Message}");
                    
                    subscriptions.RemoveAt(i);
                }
            }
        }
        
        static async Task SendMessage(IMqttClient client, string topic, string payload, MqttQualityOfServiceLevel level, bool retain)
        {
            try
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(level)
                    .WithRetainFlag(retain)
                    .Build();

                await client.PublishAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Greska pri slanju poruke: {e.Message}");
                throw;
            }
            
        }
    }
}