using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

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
                if (fromClient != null) {
                    if (fromClient.action != MessageAction.ERROR) {
                        interpretResponse(fromClient);
                    } else {
                        error(fromClient.value);
                    }
                }

                //server.Shutdown(SocketShutdown.Both);
                //server.Close();
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        public static void interpretResponse(Message response) {
            switch (response.action) {
                case MessageAction.SQL_QUERY_REQUEST:
                    SQLQuery sqlQuery = parseStatement(response.value);
                    break;
                default:
                    break;
            }
        }

        public static SQLQuery parseStatement(string statement) {
            /*
                (?=(?:[^\(]*\([^\(]*\)?)*[^\)]*$)
             
                CREATE DATABASE db;
                CREATE TABLE students %;
                CREATE INDEX idx_studID ON students (studID);
                create table stud (id, name);

                INSERT INTO students () VALUES ();
            */

            /*int from;
            int to;
            string savedStatement;
            Queue<string> queue = new Queue<string>();
            string replacedStatement = statement.Replace(";", String.Empty);
            while (replacedStatement.Contains("(")) {
                from = replacedStatement.IndexOf("(");
                to = replacedStatement.IndexOf(")", from + 1);
                savedStatement = replacedStatement.Substring(from + 1, to - from - 1); // saved on queue
                queue.Enqueue(savedStatement);
                replacedStatement = replacedStatement.Remove(from, to - from + 1).Insert(from, "%");
            }

            Console.WriteLine(replacedStatement);

            foreach (var item in queue) {
                Console.WriteLine(item);
            }*/

            /*switch (args[0].ToLower()) {
                case "create":
                    switch (args[1].ToLower()) {
                        case "database":
                            // args[2] - database name
                            break;
                        case "table":
                            
                            Console.WriteLine(">>>" + tempStatement);
                            // args[2] - table name
                            // split by comma
                            // split by whitespace
                            break;
                        case "index":
                            break;
                        default:
                            // wrong command
                            break;
                    }
                    break;
                case "drop":
                    break;
                default:
                    // wrong command
                    break;
            }*/

            return new SQLQuery();
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
            int received = server.Receive(bytes);
            string response = Encoding.ASCII.GetString(bytes, 0, received);

            Message message = parseReceived(response);
            return message;
        }

        public static void send(Message message) {
            byte[] _message = Encoding.ASCII.GetBytes(message.ToString());
            server.Send(_message);
        }

        public static void error(string message) {
            Console.Clear();
            Console.WriteLine("Eroare: ");
            Console.WriteLine(message);
        }
    }
}