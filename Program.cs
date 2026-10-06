using System.Text.RegularExpressions;
using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTGitProject
{
    internal class Program 
    {
        static async Task Main(string[] args)
        {
            // REGEX za unose
            var subscriberTopicRegex  =  @"^[^/#+\s]+(?:/(?:[^/#+\s]+|\+))*?(?:/#)?$";
            var publisherTopicRegex   =  @"^[^/#+\s]+(?:/[^/#+\s]+)*$";
            
            // Promenljive
            string topic = String.Empty;
            int qos = 0;
            MqttQualityOfServiceLevel level = MqttQualityOfServiceLevel.AtMostOnce;
            
            //MessageLogger testLoggerConsole = new MessageLogger(LogType.Console);
            IMessageLogger testLogger = new CompositeLogger();
            
            // Kreiranje factory-a i clienta
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            
            // Event handler za poruke koje pristizu
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                testLogger.LogMessageAsync(e.ApplicationMessage.Topic, 
                    e.ApplicationMessage.ConvertPayloadToString(), 
                    e.ApplicationMessage.QualityOfServiceLevel.ToString(), 
                    (e.ApplicationMessage.Retain ? "Retain" : "No Retain"));
                
                return Task.CompletedTask;
            };
            
            // Opcije za povezivanje
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();
            
            // Povezivanje sa brokerom
            try
            {
                await mqttClient.ConnectAsync(options);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Doslo je do greske prilikom povezivanja na server: {e.Message}");
            }
            
            // Event handler za disconnect
            mqttClient.DisconnectedAsync += async e =>
            {
                Console.WriteLine();
                Console.WriteLine("MQTT konekcija je prekinuta");

                if (e.Exception != null)
                {
                    Console.WriteLine($"Razlog: {e.Exception.Message}");
                }

                while (!mqttClient.IsConnected)
                {
                    try
                    {
                        Console.WriteLine("Pokusaj ponovnog povezivanja za 5 sekundi...");
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        Console.WriteLine("Pokusavam reconnect...");

                        await mqttClient.ConnectAsync(options);

                        Console.WriteLine("Reconnect uspesan!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reconnect nije uspeo: {ex.Message}");
                    }
                }
            };
            
            // MAIN LOOP
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
                    if (!Regex.IsMatch(topic, publisherTopicRegex))
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
                            return;
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
                            return;
                    }

                    await SendMessage(mqttClient, topic, payload, level, retain);
                }
                else if (opcija == "s")
                {
                    Console.Write("Unesite topic: ");
                    topic = Console.ReadLine();
                    if (!Regex.IsMatch(topic, subscriberTopicRegex))
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
                            return;
                    }
                    
                    try
                    {
                        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(topic, level)
                            .Build();
                        await mqttClient.SubscribeAsync(subscribeOptions);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Doslo je do greske prilikom subscribe-a: {e.Message}");
                    }
                    
                    Console.WriteLine($"Subscribe uspesno obavljen.");
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

        static async Task SendMessage(IMqttClient client, string topic, string payload, MqttQualityOfServiceLevel level, bool retain)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(level)
                .WithRetainFlag(retain)
                .Build();

            await client.PublishAsync(message);
        }
    }
}