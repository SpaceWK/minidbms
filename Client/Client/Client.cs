using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client {
    public class Program {
        public static Socket client;

        public static void Main(string[] args) {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11000);

            try {
                client = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try {
                    client.Connect(ipEndPoint);
                    Console.WriteLine("Client conectat la: {0}", client.RemoteEndPoint.ToString());

                    //receive();

                    menu();

                    //client.Shutdown(SocketShutdown.Both);
                    //client.Close();
                } catch (ArgumentNullException ane) {
                    Console.WriteLine("ArgumentNullException: {0}", ane.ToString());
                } catch (SocketException se) {
                    Console.WriteLine("SocketException: {0}", se.ToString());
                } catch (Exception e) {
                    Console.WriteLine("Unexpected exception: {0}", e.ToString());
                }
            } catch (Exception e) {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }

        public static void menu() {
            Console.Clear();
            Console.WriteLine("Alege o optiune:");
            Console.WriteLine("  1. Vizulizeaza tabele");
            Console.WriteLine("  2. Ruleaza SQL");
            Console.WriteLine();
            Console.Write("> ");
            var option = Console.ReadLine();

            Console.Clear();
            switch (int.Parse(option)) {
                case 1:
                    break;
                case 2:
                    Console.Write("Introdu instructiunea SQL: ");
                    var query = Console.ReadLine();
                    Message message = new Message(MessageAction.SQL_QUERY, query);
                    send(message);


                    Message fromServer = receive();
                    // do something with fromServer

                    break;
                default:
                    menu();
                    break;
            }
        }

        public static Message parseResponse(string response) {
            string[] parts = response.Split("|");
            return new Message((MessageAction)Enum.Parse(typeof(MessageAction), parts[0]), parts[1]);
        }

        public static Message receive() {
            byte[] bytes = new byte[1024];
            int received = client.Receive(bytes);
            string response = Encoding.ASCII.GetString(bytes, 0, received);

            Message message = parseResponse(response);
            return message;
        }

        public static void send(Message message) {
            byte[] bytes = Encoding.ASCII.GetBytes(message.ToString());
            int bytesSent = client.Send(bytes);
        }
    }
}