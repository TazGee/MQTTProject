using MQTTnet;

namespace MQTTGitProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string topic;
            Console.Write("Unesite topic: ");
            topic = Console.ReadLine();
            
            var factory = new MqttClientFactory();
            using var mqttClient = factory.CreateMqttClient();
            
            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string poruka = e.ApplicationMessage.ConvertPayloadToString();

                Console.WriteLine($"Topic: {topic}");
                Console.WriteLine($"Poruka: {poruka}");

                return Task.CompletedTask;
            };
            
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();
            await mqttClient.ConnectAsync(options);
            
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topic)
                .Build();
            await mqttClient.SubscribeAsync(subscribeOptions);
            
            Console.WriteLine($"Povezivanje je uspesno, cekaju se poruke...");
            
            Console.ReadKey();
        }
    }
}