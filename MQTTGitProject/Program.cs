using System.Text.RegularExpressions;
using MQTTGitProject.Models;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTGitProject
{
    /// <summary>
    /// Glavna klasa aplikacije
    /// </summary>
    internal class Program 
    {
        static bool reconnecting = false;
        
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

            InitializeHandlers(testLogger, mqttClient, options, subscriptions, config);
            
            await TryToConnect(mqttClient, options, subscriptions, config);

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
        
        static async Task TryToConnect(IMqttClient client, 
                                        MqttClientOptions options, 
                                        List<Subscription> subscriptions,
                                        Config.Config config)
        {
            reconnecting = true;
            do
            {
                try
                {
                    Console.WriteLine("Pokusavam povezivanje sa serverom...");

                    await client.ConnectAsync(options);
                    Console.Clear();
                    Console.WriteLine("Konekcija uspesno uspostavljena!");

                    reconnecting = false;
                    
                    await ResubscribeToAll(subscriptions, client);
                    Console.WriteLine("Uspesan resubscribe na sve topic-e!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Povezivanje nije uspelo: {ex.Message}");
                    Console.WriteLine("Pokusaj ponovnog povezivanja za 3 sekunde...");
                    await Task.Delay(TimeSpan.FromSeconds(config.ReconnectTimer));
                }
            } while (!client.IsConnected);
            
            Console.WriteLine("\nPovezivanje uspesno!");
        }
        
        static void InitializeHandlers(IMessageLogger logger, 
                                        IMqttClient client, 
                                        MqttClientOptions options, 
                                        List<Subscription> subscriptions,
                                        Config.Config config)
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

                    if(!reconnecting) await TryToConnect(client, options, subscriptions, config);
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"Greska pri inicijalizaciji handlera: {e.Message}");
            }
            
        }

        static async Task ResubscribeToAll(List<Subscription> subscriptions, IMqttClient client)
        {
            foreach (Subscription s in subscriptions)
            {
                try
                {
                    var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(s.Topic, s.QoS)
                        .Build();
                    await client.SubscribeAsync(subscribeOptions);
                    
                    Console.WriteLine($"Uspesno re-subscribovan na {s.Topic}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Nije moguce ponovo povezati se na {s.Topic}");
                    Console.WriteLine($"Razlog: {e.Message}");
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