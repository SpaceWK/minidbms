using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client {
    public class Program {
        public static Socket client;

        public static string currentDatabase;
        public static List<string> databasesList = new List<string>();

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

        public static void menu(bool displayLastAction = false, Message lastAction = null, int selectedOption = -1) {
            Console.Clear();
            if (displayLastAction) {
                if (lastAction.action == MessageAction.ERROR) {
                    Console.WriteLine("Eroare: {0}", lastAction.value);
                } else if (lastAction.action == MessageAction.SUCCESS) {
                    Console.WriteLine(lastAction.value);
                }

                Console.WriteLine();
                Console.WriteLine("------------------------------");
                Console.WriteLine();
            }
            Console.WriteLine("Conectat la serverul ({0}).", client.RemoteEndPoint.ToString());
            Console.WriteLine();
            Console.WriteLine("Alegeti o optiune:");
            Console.WriteLine("  1. Lista baze de date");
            Console.WriteLine("  2. Vizulizeaza tabele");
            Console.WriteLine("  3. Ruleaza SQL");
            Console.WriteLine("  4. Iesi din program");
            Console.WriteLine();
            Console.Write("> ");
            string option;
            if (selectedOption != -1) {
                option = selectedOption.ToString();
            } else {
                option = Console.ReadLine();
            }

            Console.Clear();

            switch (int.Parse(option)) {
                case 1:
                    if (databasesList.Count() > 0) {
                        Console.WriteLine("Lista baze de date:");
                        foreach (string item in databasesList) {
                            Console.WriteLine("  - {0}", item);
                        }
                        backMenu();
                    } else {
                        send(new Message(MessageAction.GET_DATABASES_REQUEST, null));

                        receiveFromServer();
                    }
                    break;
                case 2:
                    // TODO
                    break;
                case 3:
                    if (currentDatabase != null) {
                        Console.WriteLine("Baza de date: {0}", currentDatabase);
                    } else {
                        Console.WriteLine("Baza de date: Nu este selectata.");
                    }
                    Console.WriteLine();
                    Console.Write("Introdcuceti instructiunea SQL: ");
                    var query = Console.ReadLine();
                    send(new Message(MessageAction.SQL_QUERY_REQUEST, query));

                    receiveFromServer();

                    break;
                case 4:
                    send(new Message(MessageAction.CLOSE_CONNECTION, ""));
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();

                    Environment.Exit(0);
                    break;
                default:
                    menu();
                    break;
            }
        }

        public static void backMenu() {
            Console.WriteLine();
            Console.WriteLine("Apasa ENTER pentru a reveni la meniul principal.");
            ConsoleKeyInfo key = Console.ReadKey();
            if (key.Key == ConsoleKey.Enter) {
                menu();
            }
        }

        public static void interpretResponse(Message response) {
            switch (response.action) {
                case MessageAction.SQL_QUERY_RESPONSE:
                    Console.WriteLine(response.value);
                    break;
                case MessageAction.GET_DATABASES_RESPONSE:
                    string[] databases = response.value.Split(",");
                    foreach (string item in databases) {
                        databasesList.Add(item);
                    }
                    menu(false, null, 1);
                    break;
                case MessageAction.SELECT_DATABASE:
                    currentDatabase = response.value;
                    menu(false, null); // After selection, go directly to option 3: menu(false, null, 3);
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

        public static void receiveFromServer() {
            while (true) {
                Message fromServer = receive();
                if (fromServer != null) {
                    if (fromServer.action != MessageAction.ERROR && fromServer.action != MessageAction.SUCCESS) {
                        interpretResponse(fromServer);
                    } else {
                        menu(true, fromServer);
                    }
                }
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