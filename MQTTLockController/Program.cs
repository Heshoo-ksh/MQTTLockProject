using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTLockController
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== MQTT Controller Client ===");

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();
            var tcs = new TaskCompletionSource<bool>(); // TaskCompletionSource to wait for a response


            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .Build();


            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
                Console.WriteLine($"Status Update: {message}");
                tcs.SetResult(true); // Signal that the response has been received
                return Task.CompletedTask;
                 
            };

            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                Console.WriteLine("Connected to MQTT broker.");

                //Subscribe to the status topic AFTER connecting
                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("lock/status").Build());
                Console.WriteLine("Subscribed to 'lock/status' topic.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            }

            while (true)
            {
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1: Lock");
                Console.WriteLine("2: Unlock");
                Console.WriteLine("3: Activate Temporary Password");
                Console.WriteLine("4: Deactivate Temporary Password");
                Console.WriteLine("5: Exit");
                Console.Write("Enter your choice: ");

                var choice = Console.ReadLine();
                string payload = "";
                string password;

                switch (choice)
                {
                    case "1":
                        Console.Write("Enter password: ");
                        password = Console.ReadLine();
                        payload = $"lock:{password}";
                        break;
                    case "2":
                        Console.Write("Enter password: ");
                        password = Console.ReadLine();
                        payload = $"unlock:{password}";
                        break;
                    case "3":
                        Console.Write("Enter permanent password: ");
                        password = Console.ReadLine();
                        payload = $"activateTemp:{password}";
                        break;
                    case "4":
                        Console.Write("Enter permanent password: ");
                        password = Console.ReadLine();
                        payload = $"deactivateTemp:{password}";
                        break;
                    case "5":
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

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("lock/commands")
                    .WithPayload(payload)
                    .Build();

                await mqttClient.PublishAsync(message, CancellationToken.None);
                Console.WriteLine($"Sent command: '{GetUserFriendlyCommand(payload)}', waiting for the lock status...");

                await tcs.Task; // Wait here for the status update to be received
                tcs = new TaskCompletionSource<bool>(); // Reset the TaskCompletionSource for the next loop

            }
        }

        private static string GetUserFriendlyCommand(string commandPayload)
        {
            var parts = commandPayload.Split(':');
            if (parts.Length == 2)
            {
                var command = parts[0];
                return command switch
                {
                    "lock" => "Locking the door",
                    "unlock" => "Unlocking the door",
                    "activateTemp" => "Activating temporary password",
                    "deactivateTemp" => "Deactivating temporary password",
                    _ => "Unknown command"
                };
            }
            return "Invalid command format";
        }
    }
}
