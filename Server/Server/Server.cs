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

                receive();

                //server.Shutdown(SocketShutdown.Both);
                //server.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static string receive() {
            byte[] bytes = new byte[1024];
            int bytesRec = server.Receive(bytes);
            var data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

            Console.WriteLine("SERVER: {0}", data);
            return data;
        }

        public static void send(Message message) {
            byte[] _message = Encoding.ASCII.GetBytes(message.ToString());
            server.Send(_message);
        }
    }
}