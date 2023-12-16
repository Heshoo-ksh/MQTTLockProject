using System;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using System.Threading.Tasks;

namespace MQTTLockClient
{
    class Program
    {
        private static string permanentPassword = "12345"; // Example permanent password
        private static string temporaryPassword = "temp123"; // Example temporary password
        private static bool isTemporaryPasswordActive = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("===This is the Lock Client====.");

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
                        if (command == "lock" || command == "unlock")
                        {
                            Console.WriteLine($"{command}ing the door...");
                            // Simulate lock/unlock action
                            // Here you can add logic to change a file's content or another form of state representation
                            statusMessage = $"{command} operation successful";

                            if (command == "unlock" && password == temporaryPassword)
                            {
                                isTemporaryPasswordActive = false; // Disable temp password after use
                            }
                        }
                        else if (command == "activateTemp")
                        {
                            isTemporaryPasswordActive = true;
                            Console.WriteLine("Temporary password activated.");
                            statusMessage = "Temporary password activated";
                        }
                        else if (command == "deactivateTemp")
                        {
                            isTemporaryPasswordActive = false;
                            Console.WriteLine("Temporary password deactivated.");
                            statusMessage = "Temporary password deactivated";
                        }
                        else
                        {
                            statusMessage = "Invalid command";
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unauthorized attempt!");
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
            }

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
