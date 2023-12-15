using System.Text;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTLockController
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("===This is the Controller Client====.");

            // Initialize an MQTT client instance
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            //Define Broker Connection Options: Specify the broker's address and port. For local testing, you can use localhost (127.0.0.1) and the default port 1883 (we will nbeed to change this later).
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883) // Use localhost and default MQTT port
                .Build();

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

            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1: Lock");
                Console.WriteLine("2: Unlock");
                Console.WriteLine("3: Exit");
                Console.Write("Enter your choice: ");

                var choice = Console.ReadLine();

                string payload = "";
                switch (choice)
                {
                    case "1":
                        payload = "lock";
                        break;
                    case "2":
                        payload = "unlock";
                        break;
                    case "3":
                        Console.WriteLine("Exiting...");
                        if (mqttClient.IsConnected)
                        {
                            await mqttClient.DisconnectAsync();
                        }
                        return;
                    default:
                        Console.WriteLine("Invalid choice, please try again.");
                        continue;
                }

                //Publish Command Functionality
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("lock/commands")
                    .WithPayload(payload)
                    .Build();

                await mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine($"Command '{payload}' sent.");
            }
        
    }
    }
}
