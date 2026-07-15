using System.Net.NetworkInformation;

namespace WebReset.Utils {
    public class Network {

        //Ping IP to trigger Reset
        public bool WaitForPing(string ip) {
            using Ping ping = new Ping();

            while (true) {
                try {
                    PingReply reply = ping.Send(ip, 3000);

                    if (reply.Status == IPStatus.Success) {
                        Console.WriteLine($"Ping successfully: {reply.Address}");
                        return true;
                    }

                    Console.WriteLine($"Ping failed: {reply.Status}");
                } catch (PingException ex) {
                    Console.WriteLine(ex.Message);
                }

                Thread.Sleep(1000); 
            }
        }

        public bool WaitForDisconnect(string ip) {
            using Ping ping = new Ping();

            while (true) {
                try {
                    PingReply reply = ping.Send(ip, 3000);

                    if (reply.Status != IPStatus.Success)
                    {
                        Console.WriteLine("Device disconnected.");
                        return true;
                    }
                }
                catch {
                    Console.WriteLine("Device disconnected.");
                    return true;
                }

                Thread.Sleep(1000);
            }
        }
    }
}