using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTGitProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Promenljive
            string topic = String.Empty;
            int qos = 0;
            MqttQualityOfServiceLevel level = MqttQualityOfServiceLevel.AtMostOnce;
            
            // Kreiranje factory-a i clienta
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            
            // Event handler za poruke koje pristizu
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine($"\n======================================");
                Console.WriteLine($"Topic: {e.ApplicationMessage.Topic}");
                Console.WriteLine($"Poruka: {e.ApplicationMessage.ConvertPayloadToString()}");
                Console.WriteLine($"QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"Retain: {(e.ApplicationMessage.Retain ? "Retain" : "No Retain")}");
                Console.WriteLine($"Timestamp: {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}");
                Console.WriteLine($"======================================\n");

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
                    Console.Write("Unesite payload poruke: ");
                    payload = Console.ReadLine();
                
                    Console.WriteLine("1 - At Most Once\n2 - At Least Once\n3 - Exactly Once");
                    Console.Write("Unesite QoS: ");
                    qos =  int.Parse(Console.ReadLine());
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
                
                    Console.WriteLine("1 - Retain\n2 - No Retain");
                    Console.Write("Unesite retain: ");
                    tmpretain =  int.Parse(Console.ReadLine());
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

                    Console.WriteLine("1 - At Most Once\n2 - At Least Once\n3 - Exactly Once");
                    Console.Write("Unesite QoS: ");
                    qos =  int.Parse(Console.ReadLine());

                    // QoS odredjivanje
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
                    
                    // Subscribe na topic
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