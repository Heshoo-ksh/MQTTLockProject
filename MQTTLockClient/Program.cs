using System;
using System.IO;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace MQTTLockClient
{
    class Program
    {
        private static readonly string lockStateFilePath = "lockstate.txt";
        private static string permanentPassword = "12345"; // Example permanent password
        private static string temporaryPassword = "temp123"; // Example temporary password
        private static bool isTemporaryPasswordActive = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Current working directory: " + Directory.GetCurrentDirectory());

            Console.WriteLine("=== This is the Lock Client ===");

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .Build();

            mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.ToArray());
                var parts = message.Split(':');
                string statusMessage;

                if (parts.Length == 2)
                {
                    var command = parts[0];
                    var password = parts[1];

                    if ((password == permanentPassword) || (isTemporaryPasswordActive && password == temporaryPassword))
                    {
                        switch (command)
                        {
                            case "lock":
                            case "unlock":
                                UpdateLockState(command == "lock" ? "1" : "0");
                                statusMessage = $"{command} operation successful";
                                if (command == "unlock" && password == temporaryPassword)
                                {
                                    isTemporaryPasswordActive = false; // Disable temp password after use
                                }
                                break;
                            case "activateTemp":
                                isTemporaryPasswordActive = true;
                                statusMessage = "Temporary password activated";
                                break;
                            case "deactivateTemp":
                                isTemporaryPasswordActive = false;
                                statusMessage = "Temporary password deactivated";
                                break;
                            default:
                                statusMessage = "Invalid command";
                                break;
                        }
                    }
                    else
                    {
                        statusMessage = "Unauthorized attempt";
                    }
                }
                else
                {
                    statusMessage = "Invalid command format";
                }

                // Publish status message
                var statusPayload = new MqttApplicationMessageBuilder()
                    .WithTopic("lock/status")
                    .WithPayload(statusMessage)
                    .Build();
                await mqttClient.PublishAsync(statusPayload, CancellationToken.None);
            };

            try
            {
                await mqttClient.ConnectAsync(options, CancellationToken.None);
                Console.WriteLine("Connected to MQTT broker.");

                await mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("lock/commands").Build());
                Console.WriteLine("Subscribed to 'lock/commands' topic.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();

            if (mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
            }
        }

        private static void UpdateLockState(string state)
        {
            try
            {
                File.WriteAllText(lockStateFilePath, state);
                Console.WriteLine($"Lock state updated to: {(state == "1" ? "Locked" : "Unlocked")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update lock state: {ex.Message}");
            }
        }

    }
}
