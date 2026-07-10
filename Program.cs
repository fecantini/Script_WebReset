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
                Console.WriteLine("Invalid IP.");
                return;
            }

            Console.Write("Show window? [y/n]: ");
            bool showWindow = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.Write("Delete application? [y/n]: ");
            bool deleteApp = Console.ReadLine()?.Trim().ToLower() == "y";

            Console.Write("Username: ");
            string? username = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username)) {
                Console.WriteLine("Invalid username.");
                return;
            }

            Console.Write("Password: ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(password)) {
                Console.WriteLine("Invalid password.");
                return;
            }

            Console.Write("MQTT IP: ");
            string? mqttIp = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(mqttIp)) {
                Console.WriteLine("Invalid MQTT IP.");
                return;
            }

            Console.Write("MQTT Topic: ");
            string? topic = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(topic)) {
                Console.WriteLine("Invalid topic.");
                return;
            }

            Console.Write("MQTT Port: ");
            string? mqttPortString = Console.ReadLine();

            if (!int.TryParse(mqttPortString, out int mqttPort)) {
                Console.WriteLine("Invalid port.");
                return;
            }

            MqttSubscriber mqtt = new MqttSubscriber();

            if (!await mqtt.Initialize(mqttIp, mqttPort, topic)) {
                Console.WriteLine("Could not connect to MQTT broker.");
                return;
            }

            mqtt.MessageReceived += message => {

                if (message.Trim() != "5") {
                    Console.WriteLine("CPU is not ready");
                    return;
                }

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

                    Console.WriteLine(
                        $"Counter Reset: {counterReset} | Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                } finally {
                    driver.Quit();
                }
            };

            Console.WriteLine("Waiting MQTT messages...");
            Console.ReadLine();

            await mqtt.Disconnect();
        }
    }
}