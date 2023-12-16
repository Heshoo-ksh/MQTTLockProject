using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;

namespace MQTTLockController
{
    class Program
    {
        private static bool isLocked = true;
        private static bool isTemporaryPasswordActive = false;
        private static string temporaryPassword = "";
        private static string permanentPassword = "1234"; 
        private static bool isTemporaryPasswordUsed = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("===This is the Controller Client====.");

            var factory = new MqttFactory();
            var mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", 1883) 
                .Build();

            try
            {
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
                Console.WriteLine("4: Show Temporary Password");
                Console.WriteLine("5: Deactivate Temporary Password");
                Console.WriteLine("6: Exit");
                Console.Write("Enter your choice: ");

                var choice = Console.ReadLine();

                string payload = "";
                switch (choice)
                {
                    case "1":
                        payload = "lock";
                        isLocked = true;
                        break;
                    case "2":
                        if (isTemporaryPasswordActive)
                        {
                            Console.Write("Enter temporary password: ");
                            var tempPasswordInput = Console.ReadLine();
                            if (CheckTempPassword(tempPasswordInput) && !isTemporaryPasswordUsed)
                            {
                                payload = "unlock";
                                isLocked = false;
                                isTemporaryPasswordActive = false;
                                isTemporaryPasswordUsed = true;
                            }
                            else
                            {
                                Console.WriteLine("Incorrect temporary password or password already used. Unable to unlock.");
                                continue;
                            }
                        }
                        else
                        {
                            Console.Write("Enter password: ");
                            var password = Console.ReadLine();
                            if (CheckPassword(password))
                            {
                                payload = "unlock";
                                isLocked = false;
                                isTemporaryPasswordActive = false;
                            }
                            else
                            {
                                Console.WriteLine("Incorrect password. Unable to unlock.");
                                continue;
                            }
                        }
                        break;
                    case "3":
                        if (!isTemporaryPasswordActive)
                        {
                            Console.Write("Enter permanent password to activate temporary password: ");
                            var activatePassword = Console.ReadLine();
                            if (CheckPassword(activatePassword))
                            {
                                temporaryPassword = GenerateTemporaryPassword();
                                isTemporaryPasswordActive = true;
                                isTemporaryPasswordUsed = false;
                                Console.WriteLine($"Temporary password activated: {temporaryPassword}");
                            }
                            else
                            {
                                Console.WriteLine("Incorrect permanent password. Unable to activate temporary password.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Temporary password is already active. Deactivate it first before activating a new one.");
                        }
                        continue;
                    case "4":
                        Console.Write("Enter permanent password to display temporary password: ");
                        var showPassword = Console.ReadLine();
                        if (CheckPassword(showPassword))
                        {
                            ShowTempPassword();
                        }
                        else
                        {
                            Console.WriteLine("Incorrect permanent password. Unable to show temporary password.");
                        }
                        continue;
                    case "5":
                        Console.Write("Enter permanent password to deactivate temporary password: ");
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
                    case "6":
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

        private static bool CheckPassword(string inputPassword)//this will check if permenant pass is correct 
        {
            return inputPassword == permanentPassword;
        }

        private static bool CheckTempPassword(string inputTempPassword)//this will check if temp pass is correct 
        {
            return inputTempPassword == temporaryPassword;
        }

        private static string GenerateTemporaryPassword()// this function will create a random temp pass each time its called , so temp pass can be only used once .,
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string tempPassword = new string(Enumerable.Repeat(chars, 8)
                                                .Select(s => s[random.Next(s.Length)]).ToArray());
            return tempPassword;
        }

        private static void ShowTempPassword() // this will show the temp pass if its curretly in use, or if the user wants to intilize one. 
        {
            if (isTemporaryPasswordActive)
            {
                Console.WriteLine($"Temporary Password: {temporaryPassword}");
            }
            else
            {
                Console.WriteLine("Temporary password is not currently active.");
            }
        }
    }
}
