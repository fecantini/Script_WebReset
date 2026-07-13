using OpenQA.Selenium;
using WebReset.Drivers;
using WebReset.MQTT;
using WebReset.Pages;

namespace WebReset {
    internal class Program {
        static async Task Main(string[] args) {
            int counterReset = 0;

            Console.Write("Device IP: ");
            string? ip = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(ip)) {
                Console.WriteLine("Invalid IP");
                return;
            }

            bool showWindow = ReadYesNo("Show window? [y/n]: ");
            bool deleteApp = ReadYesNo("Delete application? [y/n]: ");

            Console.Write("Username: ");
            string? username = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username)) {
                Console.WriteLine("Invalid username");
                return;
            }

            Console.Write("Password: ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(password)) {
                Console.WriteLine("Invalid password");
                return;
            }

            Console.Write("MQTT IP: ");
            string? mqttIp = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(mqttIp)) {
                Console.WriteLine("Invalid MQTT IP");
                return;
            }

            Console.Write("MQTT Topic: ");
            string? topic = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(topic)) {
                Console.WriteLine("Invalid topic");
                return;
            }

            Console.Write("MQTT Port: ");
            string? mqttPortString = Console.ReadLine();

            if (!int.TryParse(mqttPortString, out int mqttPort)) {
                Console.WriteLine("Invalid port");
                return;
            }

            Console.Write("MQTT Trigger: ");
            string? mqttTrigger = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(mqttTrigger)) {
                Console.WriteLine("Invalid trigger");
                return;
            }

            MqttSubscriber mqtt = new MqttSubscriber();

            if (!await mqtt.Initialize(mqttIp, mqttPort, topic)) {
                Console.WriteLine("Could not connect to MQTT broker.");
                return;
            }

            string? lastMessage = null;
            bool resetRunning = false;

            object lockObject = new();

            mqtt.MessageReceived += message => {
                string currentMessage = message.Trim();

                lock (lockObject) {
                    if (currentMessage == lastMessage) {
                        Console.WriteLine("Duplicate MQTT message ignored.");
                        return;
                    }

                    lastMessage = currentMessage;

                    if (resetRunning) {
                        Console.WriteLine("Reset already running. Message ignored.");
                        return;
                    }

                    if (currentMessage != mqttTrigger) {
                        Console.WriteLine("CPU is not ready");
                        return;
                    }

                    resetRunning = true;
                }

                _ = Task.Run(() => {
                    try {
                        ExecuteReset(
                            ip,
                            username,
                            password,
                            deleteApp,
                            showWindow,
                            ref counterReset
                        );
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Reset error: {ex.Message}");
                    }
                    finally {
                        lock (lockObject) {
                            resetRunning = false;
                        }
                    }
                });
            };

            Console.WriteLine("Waiting MQTT messages...");
            Console.ReadLine();

            await mqtt.Disconnect();
        }

        static bool ReadYesNo(string message) {
            while (true) {
                Console.Write(message);

                string? input = Console.ReadLine()?.Trim().ToLower();

                if (input == "y")
                    return true;

                if (input == "n")
                    return false;

                Console.WriteLine("Invalid option. Use y or n.");
            }
        }

        static void ExecuteReset(
            string ip,
            string username,
            string password,
            bool deleteApp,
            bool showWindow,
            ref int counterReset) {
            Console.WriteLine("CPU is INACTIVE");

            IWebDriver driver = Drive.CreateDriver(ip, showWindow);

            try {
                Page page = new Page(driver);

                if (!page.ManagementTab()) {
                    Console.WriteLine("Failed to open Management tab.");
                    return;
                }

                if (!page.Login(username, password)) {
                    Console.WriteLine("Login failed.");
                    return;
                }

                if (!page.Reset(deleteApp, username, password)) {
                    Console.WriteLine("Reset failed.");
                    return;
                }

                counterReset++;

                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine();
                Console.WriteLine(new string('=', 60));
                Console.WriteLine($"RESET #{counterReset}");
                Console.WriteLine($"Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine(new string('=', 60));

                Console.ResetColor();
            }
            finally {
                driver.Quit();
            }
        }
    }
}
