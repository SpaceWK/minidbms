using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Xml.Linq;

namespace Client {
    public class Program {
        public static Socket client;

        public static string currentDatabase;
        public static List<string> databasesList = new List<string>();
        public static List<string> tablesList = new List<string>();
        public static Dictionary<string, List<string>> tableData = new Dictionary<string, List<string>>();

        public static double selectTime = 0.0;
        public static System.Timers.Timer selectTimer = new System.Timers.Timer();
        public static Dictionary<string, List<string>> selectData = new Dictionary<string, List<string>>();

        public static void Main(string[] args) {
            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11000);

            selectTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimerEvent);
            selectTimer.Interval = 1;

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

        private static void TimerEvent(object source, ElapsedEventArgs e) {
            selectTime += 0.1;
        }

        public static void menu(bool displayLastAction = false, Message lastAction = null, int selectedOption = -1) {
            Console.Clear();
            if (displayLastAction) {
                if (lastAction.action == MessageAction.ERROR) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Eroare: {0}", lastAction.value);
                } else if (lastAction.action == MessageAction.SUCCESS) {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(lastAction.value);
                } else if (lastAction.action == MessageAction.SUCCESS_SELECT) {
                    displaySelectData(lastAction.value);
                }
                Console.ResetColor();

                Console.WriteLine();
                for (int i = 0; i < Console.WindowWidth; i++) {
                    Console.Write("-");
                }
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Conectat la serverul ({0}).", client.RemoteEndPoint.ToString());
            Console.ResetColor();
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
                    if (currentDatabase != null) {
                        tablesMenu();
                    } else {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Baza de date: Nu este selectata.");
                        Console.ResetColor();
                        backMenu();
                    }
                    break;
                case 3:
                    if (currentDatabase != null) {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        Console.WriteLine("Baza de date: {0}", currentDatabase);
                    } else {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Baza de date: Nu este selectata.");
                    }
                    Console.ResetColor();

                    Console.WriteLine();
                    Console.Write("Introduceti instructiunea SQL: ");
                    var query = Console.ReadLine();
                    send(new Message(MessageAction.SQL_QUERY_REQUEST, query));

                    if (query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) {
                        selectTime = 0.0;
                        selectTimer.Start();
                    }

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

        public static void displaySelectData(string message) {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Timp: {0}s", selectTime.ToString("0.00"));
            selectTimer.Stop();
            selectTime = 0.0;
            Console.ResetColor();

            Console.WriteLine();
            for (int i = 0; i < Console.WindowWidth; i++) {
                Console.Write("-");
            }
            Console.WriteLine();


            if (message != "NO_RESULTS") {
                parseData(message, selectData);
                printData(selectData);
            } else {
                Console.WriteLine("Nu exista rezultate.");
            }


            selectData.Clear();
            backMenu();
        }

        public static void tablesMenu() {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Baza de date: {0}", currentDatabase);
            Console.ResetColor();

            Console.WriteLine();
            for (int i = 0; i < Console.WindowWidth; i++) {
                Console.Write("-");
            }
            Console.WriteLine();
            if (tableData.Count() > 0) {
                printData(tableData);

                tableData.Clear();
                backMenu();
            } else {
                if (tablesList.Count() > 0) {
                    Console.WriteLine("Lista tabele:");
                    foreach (string item in tablesList) {
                        Console.WriteLine("  - {0}", item);
                    }

                    Console.WriteLine();
                    Console.Write("Introduceti numele tabelei: ");
                    var tableName = Console.ReadLine();
                    send(new Message(MessageAction.GET_TABLE_DATA_REQUEST, tableName));

                    receiveFromServer();
                } else {
                    send(new Message(MessageAction.GET_TABLES_REQUEST, currentDatabase));

                    receiveFromServer();
                }
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

        public static void parseData(string data, Dictionary<string, List<string>> dictionary) {
            // DName:Databases 1^Data Structures^C Programming
            // DiscID,DName:DB1#Databases 1^DB#Data Structures^CP#C Programming

            string[] parts = data.Split(":");
            string[] tableFieldNames = parts[0].Split(",");
            string[] tableFieldValues = parts[1].Split("^");

            List<string> fieldValues;
            int index = 0;
            foreach (string name in tableFieldNames) {
                fieldValues = new List<string>();

                foreach (string item in tableFieldValues) {
                    if (item.Contains("#")) {
                        string[] values = item.Split("#");
                        fieldValues.Add(values[index]);
                    } else {
                        fieldValues.Add(item);
                    }
                }

                dictionary.Add(name, fieldValues);
                index++;
            }
        }

        public static void printData(Dictionary<string, List<string>> dictionary) {
            foreach (KeyValuePair<string, List<string>> item in dictionary) {
                Console.Write(item.Key.PadRight(30));
            }
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;

            for (int i = 0; i < dictionary.FirstOrDefault().Value.Count(); i++) {
                foreach (KeyValuePair<string, List<string>> item in dictionary) {
                    Console.Write(dictionary[item.Key][i].PadRight(30));
                }

                Console.WriteLine();
            }
            Console.ResetColor();
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
                case MessageAction.GET_TABLES_RESPONSE:
                    string[] tables = response.value.Split(",");
                    foreach (string item in tables) {
                        tablesList.Add(item);
                    }
                    menu(false, null, 2);
                    break;
                case MessageAction.GET_TABLE_DATA_RESPONSE:
                    parseData(response.value, tableData);

                    menu(false, null, 2);
                    break;
                case MessageAction.SELECT_DATABASE:
                    currentDatabase = response.value;
                    menu(false, null); // After selection, go directly to option 3: menu(false, null, 3);
                    break;
                case MessageAction.SUCCESS_SELECT:
                    menu(true, response);
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