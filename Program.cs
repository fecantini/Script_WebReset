using OpenQA.Selenium;
using WebReset.Drivers;
using WebReset.MQTT;
using WebReset.Pages;
using WebReset.Utils;

namespace WebReset {
    internal class Program {
        static async Task Main(string[] args) {
            ResetCounter counterReset = new ResetCounter();

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

            Console.Write("Archive for log (.txt): ");
            string? logTxt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(logTxt)) {
                Console.WriteLine("Invalid archive");
                return;
            }

            bool mqttReset = ReadYesNo("Reset via MQTT? [y/n]: ");

            if (mqttReset) {
                await RunMqttReset(
                    ip,
                    username,
                    password,
                    deleteApp,
                    showWindow,
                    logTxt,
                    counterReset
                );
            } else {
                RunPingReset(
                    ip,
                    username,
                    password,
                    deleteApp,
                    showWindow,
                    logTxt,
                    counterReset
                );
            }
        }


        static async Task RunMqttReset(
            string ip,
            string username,
            string password,
            bool deleteApp,
            bool showWindow,
            string logTxt,
            ResetCounter counterReset) {

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

            bool resetRunning = false;
            bool resetDoneForCurrentTrigger = false;

            DateTime nextRetry = DateTime.MinValue;

            object lockObject = new();

            mqtt.MessageReceived += message => {
                string currentMessage = message.Trim();

                lock (lockObject) {

                    if (currentMessage != mqttTrigger) {
                        resetDoneForCurrentTrigger = false;

                        Console.WriteLine("CPU is not ready!");
                        return;
                    }

                    if (resetRunning) {
                        Console.WriteLine("Reset already running, message ignored");
                        return;
                    }

                    if (resetDoneForCurrentTrigger) {
                        Console.WriteLine("Reset already executed for current trigger");
                        return;
                    }

                    if (DateTime.Now < nextRetry) {
                        TimeSpan remaining = nextRetry - DateTime.Now;
                        Console.WriteLine($"Waiting {remaining.Seconds}s before next reset attempt.");
                        return;
                    }

                    resetRunning = true;
                }

                _ = Task.Run(() => {
                    try {
                        bool success = ExecuteReset(
                            ip,
                            username,
                            password,
                            deleteApp,
                            showWindow,
                            logTxt,
                            counterReset
                        );

                        lock (lockObject) {
                            if (success) {
                                resetDoneForCurrentTrigger = true;
                                nextRetry = DateTime.MinValue;
                            } else {
                                 nextRetry = DateTime.Now.AddSeconds(5);
                                 Console.WriteLine($"Retry available at {nextRetry:HH:mm:ss}");
                            }
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Reset error: {ex.Message}");

                        lock (lockObject) {
                            nextRetry = DateTime.Now.AddSeconds(5);
                        }
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


        static void RunPingReset(
            string ip,
            string username,
            string password,
            bool deleteApp,
            bool showWindow,
            string logTxt,
            ResetCounter counterReset) {

            Network network = new Network();

            while (true) {

                Console.WriteLine("Waiting device availability...");

                if (network.WaitForPing(ip)) {

                    bool success = ExecuteReset(
                        ip,
                        username,
                        password,
                        deleteApp,
                        showWindow,
                        logTxt,
                        counterReset
                    );

                    if (!success) {
                        Console.WriteLine("Reset failed. Waiting for next attempt...");
                        Thread.Sleep(3000);
                        continue;
                    }

                    Console.WriteLine("Waiting device shutdown...");

                    network.WaitForDisconnect(ip);


                    Console.WriteLine("Waiting device restart...");

                    network.WaitForPing(ip);

                    Console.WriteLine("Device available again");
                }
            }
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


        static bool ExecuteReset(
            string ip,
            string username,
            string password,
            bool deleteApp,
            bool showWindow,
            string logTxt,
            ResetCounter counterReset) {

            Console.WriteLine("CPU is ready!!!");

            IWebDriver driver = Drive.CreateDriver(ip, showWindow);

            try {

                Page page = new Page(driver);


                if (!page.ManagementTab()) {
                    Console.WriteLine("Failed to open Management tab.");
                    return false;
                }


                if (!page.Login(username, password)) {
                    Console.WriteLine("Login failed.");
                    return false;
                }


                if (!page.Reset(deleteApp, username, password)) {
                    Console.WriteLine("Reset failed.");
                    return false;
                }


                counterReset.Value++;


                string log = $@"
============================================================
RESET #{counterReset.Value}
Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss}
============================================================
";


                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(log);
                Console.ResetColor();

                File.AppendAllText(logTxt, log);

                return true;

            } finally {

                driver.Quit();

            }
        }
    }
}