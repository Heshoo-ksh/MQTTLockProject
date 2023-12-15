using System.Text;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTLockClient
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("===This is the Lock Client====.");

            // Initialize an MQTT client instance
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            //Define Broker Connection Options: Specify the broker's address and port. For local testing, you can use localhost (127.0.0.1) and the default port 1883 (we will nbeed to change this later).
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883) // Use localhost and default MQTT port
                .Build();

            //Message Handler
            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                //    var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
                if (message == "lock")
                {
                    Console.WriteLine("Locking the door...");
                    // Add further logic for 'lock' command
                }
                else if (message == "unlock")
                {
                    Console.WriteLine("Unlocking the door...");
                    // Add further logic for 'unlock' command
                }
                // Add logic to handle lock/unlock commands here
            };

            try
            {
                //Connect to the Broker
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                Console.WriteLine("Connected to MQTT broker.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            }

            //Subscribe to Command Topic (lock/commands)
            await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("lock/commands").Build());
            Console.WriteLine("Subscribed to 'lock/commands' topic.");

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }
    }
}
