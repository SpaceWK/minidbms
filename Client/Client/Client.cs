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
                    
                    menu();
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

        public static void menu(bool displayLastAction = false, Message lastAction = null) {
            Console.Clear();
            if (displayLastAction) {
                if (lastAction.action == MessageAction.ERROR) {
                    Console.WriteLine("Eroare: {0}", lastAction.value);
                } else if (lastAction.action == MessageAction.SUCCESS) {
                    Console.WriteLine(lastAction.value);
                }

                Console.WriteLine();
            }
            Console.WriteLine("Conectat la serverul ({0}).", client.RemoteEndPoint.ToString());
            Console.WriteLine();
            Console.WriteLine("Alegeti o optiune:");
            Console.WriteLine("  1. Vizulizeaza tabele");
            Console.WriteLine("  2. Ruleaza SQL");
            Console.WriteLine("  3. Iesi din program");
            Console.WriteLine();
            Console.Write("> ");
            var option = Console.ReadLine();

            Console.Clear();
            switch (int.Parse(option)) {
                case 1:
                    break;
                case 2:
                    Console.Write("Introduceti instructiunea SQL: ");
                    var query = Console.ReadLine();
                    send(new Message(MessageAction.SQL_QUERY_REQUEST, query));


                    Message fromServer = receive();
                    if (fromServer != null) {
                        if (fromServer.action != MessageAction.ERROR && fromServer.action != MessageAction.SUCCESS) {
                            interpretResponse(fromServer);
                        } else {
                            menu(true, fromServer);
                        }
                    }

                    break;
                case 3:
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();

                    Environment.Exit(0);
                    break;
                default:
                    menu();
                    break;
            }
        }

        public static void interpretResponse(Message response) {
            switch (response.action) {
                case MessageAction.SQL_QUERY_RESPONSE:
                    Console.WriteLine(response.value);
                    break;
                default:
                    break;
            }
        }

        public static Message parseReceived(string response) {
            string[] parts = response.Split("|");
            MessageAction messageAction;
            if (Enum.TryParse<MessageAction>(parts[0], out messageAction)) {
                return new Message(messageAction, parts[1]);
            } else {
                return null;
            }
        }

        public static Message receive() {
            byte[] bytes = new byte[1024];
            int received = client.Receive(bytes);
            string response = Encoding.ASCII.GetString(bytes, 0, received);

            Message message = parseReceived(response);
            return message;
        }

        public static void send(Message message) {
            byte[] bytes = Encoding.ASCII.GetBytes(message.ToString());
            int bytesSent = client.Send(bytes);
        }
    }
}