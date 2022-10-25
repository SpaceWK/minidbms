using System;
using System.Collections;
using System.Collections.Generic;
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
                    if (sqlQuery != null) {
                        executeQuery(sqlQuery);
                    } else {
                        error("Nu s-a executat query-ul.");
                    }
                    break;
                default:
                    break;
            }
        }

        public static void executeQuery(SQLQuery sqlQuery) {
            switch (sqlQuery.type) {
                case SQLQueryType.CREATE_DATABASE:
                    // sqlQuery.CREATE_DATABASE_NAME - database name;
                    if (sqlQuery.CREATE_DATABASE_NAME == "test") {
                        clientError("Baza de date '" + sqlQuery.CREATE_DATABASE_NAME + "' exista deja.");
                    }
                    break;
                case SQLQueryType.CREATE_TABLE:
                    // sqlQuery.CREATE_TABLE_NAME - table name;
                    // etc
                    break;
                case SQLQueryType.CREATE_INDEX:
                    // sqlQuery.CREATE_INDEX_NAME - index name;
                    break;
                default:
                    break;
            }
        }

        public static SQLQuery parseStatement(string statement) {
            /*
                CREATE DATABASE db;
                CREATE TABLE students (
                    studID INT,
                    groupID INT,
                    name VARCHAR,
                    tel INT,
                    email VARCHAR,
                    PRIMARY KEY (studID)
                );
                CREATE INDEX idx_studID ON students (studID, email);

                DROP DATABASE db;
                DROP TABLE students;

                INSERT INTO students () VALUES ();
            */

            statement = statement.Replace(";", String.Empty);
            string pattern = @"\(\s?(.+?\)?)\)";

            List<string> matches = Regex.Matches(statement, pattern).Cast<Match>().Select(match => match.Value).ToList();
            string replacedStatement = Regex.Replace(statement, pattern, "%");

            string[] args = replacedStatement.Split(" ");

            SQLQuery sqlQuery;
            switch (args[0].ToLower()) {
                case "create":
                    switch (args[1].ToLower()) {
                        case "database":
                            sqlQuery = new SQLQuery(SQLQueryType.CREATE_DATABASE);
                            sqlQuery.CREATE_DATABASE_NAME = args[2]; // args[2] - database name
                            return sqlQuery;
                        case "table":
                            string replaced = matches[0].Substring(1, matches[0].Length - 2); // matches[0] - table structure
                            string[] _args = replaced.Split(",", StringSplitOptions.TrimEntries);

                            Dictionary<string, string> structureItems = new Dictionary<string, string>();
                            foreach (string arg in _args) {
                                string[] item = arg.Split(" ");
                                structureItems.Add(item[0], item[1]);
                            }

                            sqlQuery = new SQLQuery(SQLQueryType.CREATE_TABLE);
                            sqlQuery.CREATE_TABLE_NAME = args[2];
                            // TODO: Set attributes
                            return sqlQuery;
                        case "index":
                            // args[2] - index name
                            // args[3] - ON
                            // args[4] - table name
                            // matches[0] - index structure
                            break;
                        default:
                            error("Query invalid.");
                            break;
                    }
                    break;
                case "drop":
                    switch (args[1].ToLower()) {
                        case "database":
                            // args[2] - database name
                            break;
                        case "table":
                            // args[2] - table name
                            break;
                        default:
                            error("Query invalid.");
                            break;
                    }
                    break;
                default:
                    error("Query invalid.");
                    break;
            }

            //return new SQLQuery(SQLQueryType.CREATE_DATABASE, args);

            return null;
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
            Console.WriteLine("Eroare: {0}", message);
        }

        public static void clientError(string message) {
            send(new Message(MessageAction.ERROR, message));
        }
    }
}