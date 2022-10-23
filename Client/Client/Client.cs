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
                    break;
                default:
                    menu();
                    break;
            }
        }

        public static string receive() {
            byte[] bytes = new byte[1024];
            int bytesRec = client.Receive(bytes);
            var data = Encoding.ASCII.GetString(bytes, 0, bytesRec);

            Console.WriteLine("CLIENT: {0}", data);
            return data;
        }

        public static void send(Message message) {
            byte[] _message = Encoding.ASCII.GetBytes(message.ToString());
            int bytesSent = client.Send(_message);
        }
    }
}