using MQTTnet;
using MQTTnet.Protocol;

namespace MQTTGitProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Unosenje topic-a koji se prati i qos-a
            string topic;
            int qos;
            
            Console.Write("Unesite topic: ");
            topic = Console.ReadLine();

            Console.WriteLine("1 - At Most Once\n2 - At Least Once\n3 - Exactly Once");
            Console.Write("Unesite QoS: ");
            qos =  int.Parse(Console.ReadLine());

            // QoS odredjivanje
            MqttQualityOfServiceLevel level;
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
            
            // Kreiranje factory-a i clienta
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            
            // Event handler za poruke koje pristizu
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                Console.WriteLine($"Topic: {e.ApplicationMessage.Topic}");
                Console.WriteLine($"Poruka: {e.ApplicationMessage.ConvertPayloadToString()}");
                Console.WriteLine($"QoS: {e.ApplicationMessage.QualityOfServiceLevel}");
                Console.WriteLine($"Retain: {(e.ApplicationMessage.Retain ? "Retain" : "No Retain")}");
                Console.WriteLine($"Timestamp: {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}");

                return Task.CompletedTask;
            };
            
            // Opcije za povezivanje
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();
            
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

                        var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(topic, level)
                            .Build();

                        await mqttClient.SubscribeAsync(subscribeOptions);

                        Console.WriteLine($"Ponovo pretplacen na topic: {topic}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Reconnect nije uspeo: {ex.Message}");
                    }
                }
            };
            
            // Povezivanje sa brokerom
            try
            {
                await mqttClient.ConnectAsync(options);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Doslo je do greske prilikom povezivanja: {e.Message}");
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
            
            Console.WriteLine($"Povezivanje je uspesno, cekaju se poruke...");
            Console.ReadKey();
        }
    }
}