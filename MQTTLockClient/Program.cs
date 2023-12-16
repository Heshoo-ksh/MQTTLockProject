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
        private static string temporaryPassword = ""; // Temporary password will be generated
        private static bool isTemporaryPasswordActive = false;
        private static bool isTemporaryPasswordUsed = false;

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

                    if ((password == permanentPassword) || (isTemporaryPasswordActive && !isTemporaryPasswordUsed && password == temporaryPassword))
                    {
                        switch (command)
                        {
                            case "lock":
                            case "unlock":
                                UpdateLockState(command == "lock" ? "1" : "0");
                                statusMessage = $"{command} operation successful";
                                if (command == "unlock" && password == temporaryPassword)
                                {
                                    isTemporaryPasswordActive = false; 
                                }
                                break;
                            case "activateTemp":
                                isTemporaryPasswordActive = true;
                                statusMessage = "Temporary password activated";
                                break;
                            case "deactivateTemp":
                                if (isTemporaryPasswordUsed == true)
                                {
                                    statusMessage = "Temporary password is not currently active or does not exists";
                                    break;
                                }
                                else
                                {
                                    isTemporaryPasswordActive = false;
                                    statusMessage = "Temporary password deactivated";
                                }
                                break;
                            default:
                                statusMessage = "Invalid command";
                                break;
                        }
                        if (isTemporaryPasswordActive && password == temporaryPassword)
                        {
                            isTemporaryPasswordUsed = true;
                            Console.WriteLine("Temporary password used and now is deactivated!");
                        }
                        else if (password == permanentPassword)
                        {
                            // Handle permanent password specific commands like activating/deactivating temporary password
                            if (command == "activateTemp")
                            {
                                temporaryPassword = GenerateTemporaryPassword();
                                isTemporaryPasswordActive = true;
                                isTemporaryPasswordUsed = false;
                                statusMessage = $"Temporary password activated: {temporaryPassword}";
                            }
                            else if (command == "deactivateTemp")
                            {
                                temporaryPassword = "";
                                isTemporaryPasswordActive = false;
                                statusMessage = "Temporary password deactivated";
                            }
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
                Console.WriteLine(statusMessage);

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
        private static string GenerateTemporaryPassword() // Generates a new temporary password
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                                      .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
