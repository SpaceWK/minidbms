using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server {
    public class Program {
        public static Socket server;

        public static void Main(string[] args) {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11000);

            try {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(ipEndPoint);
                listener.Listen(100);

                Console.WriteLine("Se asteapta o conexiune...");
                server = listener.Accept();

                Message fromClient = receive();
                // do something with fromClient

                //server.Shutdown(SocketShutdown.Both);
                //server.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static Message parseResponse(string response) {
            string[] parts = response.Split("|");
            return new Message((MessageAction)Enum.Parse(typeof(MessageAction), parts[0]), parts[1]);
        }

        public static Message receive() {
            byte[] bytes = new byte[1024];
            int received = server.Receive(bytes);
            string response = Encoding.ASCII.GetString(bytes, 0, received);

            Message message = parseResponse(response);
            return message;
        }

        public static void send(Message message) {
            byte[] _message = Encoding.ASCII.GetBytes(message.ToString());
            server.Send(_message);
        }
    }
}