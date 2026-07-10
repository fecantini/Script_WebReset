using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;

namespace WebReset.MQTT {
    public class MqttSubscriber {
        private IMqttClient? mqttClient;

        public string LastMessage { get; private set; } = string.Empty;

        public event Action<string>? MessageReceived;

        public async Task<bool> Initialize(string mqttIp, int mqttPort, string topic) {
            try {
                Console.WriteLine($"Connecting to MQTT broker ({mqttIp}:{mqttPort})...");

                var factory = new MqttClientFactory();
                mqttClient = factory.CreateMqttClient();

                mqttClient.ApplicationMessageReceivedAsync += e => {
                    LastMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    Console.WriteLine($"Message: {LastMessage}");

                    MessageReceived?.Invoke(LastMessage);

                    return Task.CompletedTask;
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(mqttIp, mqttPort)
                    .Build();

                await mqttClient.ConnectAsync(options);

                Console.WriteLine("Connected.");

                await mqttClient.SubscribeAsync(topic);

                Console.WriteLine($"Subscribed to topic: {topic}");

                return true;

            } catch (Exception ex) {
                Console.WriteLine($"MQTT error: {ex.Message}");
                return false;
            }
        }

        public async Task Disconnect() {
            if (mqttClient != null && mqttClient.IsConnected)
            {
                await mqttClient.DisconnectAsync();
                Console.WriteLine("Disconnected");
            }
        }

        public bool IsConnected =>
            mqttClient?.IsConnected ?? false;
    }
}