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
        // this will give us the current status of the lock , fals=unlocked , true=locked 
        private static bool isLocked = true;

        //this is  the current status of the temp pass, whether its activated or not . 
        private static bool isTemporaryPasswordActive = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("===This is the Controller Client====.");

            // Initialize an MQTT client instance
            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            // Define Broker Connection Options
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883)
                .Build();

            try
            {
                // Connect to the Broker
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
                Console.WriteLine("3: Activate Temporary Password");
                Console.WriteLine("4: Deactivate Temporary Password");
                Console.WriteLine("5: Exit");
                Console.Write("Enter your choice: ");

                var choice = Console.ReadLine();

                string payload = "";
                switch (choice)
                {
                    case "1":
                        // Lock the door
                        payload = "lock";
                        isLocked = true;
                        break;
                    case "2":
                        // Unlock the door if the correct password is provided
                        Console.Write("Enter password: ");
                        var password = Console.ReadLine();
                        if (CheckPassword(password))
                        {
                            payload = "unlock";
                            isLocked = false;
                            isTemporaryPasswordActive = false; // Deactivate temporary password after unlocking 
                        }
                        else
                        {
                            Console.WriteLine("Incorrect password. Unable to unlock.");
                            continue;
                        }
                        break;
                    case "3":
                        // Activate temporary password using the permanent password
                        Console.Write("Enter permanent password: ");
                        var permanentPassword = Console.ReadLine();
                        if (CheckPassword(permanentPassword))
                        {
                            isTemporaryPasswordActive = true;
                            Console.WriteLine("Temporary password activated.");
                        }
                        else
                        {
                            Console.WriteLine("Incorrect permanent password. Unable to activate temporary password.");
                        }
                        continue;
                    case "4":
                        // Deactivate temporary password using the permanent password
                        Console.Write("Enter permanent password: ");
                        var deactivatePassword = Console.ReadLine();
                        if (CheckPassword(deactivatePassword))
                        {
                            isTemporaryPasswordActive = false;
                            Console.WriteLine("Temporary password deactivated.");
                        }
                        else
                        {
                            Console.WriteLine("Incorrect permanent password. Unable to deactivate temporary password.");
                        }
                        continue;
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
                Console.WriteLine($"Command '{payload}' sent.");
            }
        }

        // this will checj if user password is correct or not,
        private static bool CheckPassword(string inputPassword)
        {
            // For simplicity, assume a fixed password (replace with your logic)
            string correctPassword = "12345";
            return inputPassword == correctPassword;
        }
    }
}
