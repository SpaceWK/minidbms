using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Server {
    public class Program {
        public static Socket server;
        public static XmlDocument catalog = new XmlDocument();

        public static string currentDatabase;

        public static string workingPath;

        public static void Main(string[] args) {
            workingPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

            string dbDirectoryPath = Path.Combine(workingPath, "Databases");
            if (!Directory.Exists(dbDirectoryPath)) {
                Directory.CreateDirectory(Path.Combine(dbDirectoryPath, "Databases"));
            }

            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11000);

            try {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(ipEndPoint);
                listener.Listen(100);

                Console.WriteLine("Se asteapta o conexiune...");
                server = listener.Accept();

                clientList();

                while (true) {
                    Message fromClient = receive();
                    if (fromClient != null) {
                        if (fromClient.action != MessageAction.ERROR) {
                            interpretResponse(fromClient);
                        } else {
                            error(fromClient.value);
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }
        public static void interpretResponse(Message response) {
            switch (response.action) {
                case MessageAction.SQL_QUERY_REQUEST:
                    SQLQuery sqlQuery = parseStatement(response.value);
                    if (sqlQuery != null && sqlQuery.error == null) {
                        Console.WriteLine("Executa query: {0}", response.value);
                        executeQuery(sqlQuery);
                    } else {
                        send(new Message(MessageAction.ERROR, sqlQuery.error));
                    }
                    break;

                case MessageAction.GET_DATABASES_REQUEST:
                    catalog.Load("../../../Catalog.xml");

                    XmlNodeList databasesNodes = catalog.SelectNodes(@"//Databases/Database");
                    if (databasesNodes != null && databasesNodes.Count > 0) {
                        List<string> databasesNames = new List<string>();
                        foreach (XmlNode item in databasesNodes) {
                            databasesNames.Add(item.Attributes["databaseName"].Value);
                        }

                        send(new Message(MessageAction.GET_DATABASES_RESPONSE, String.Join(",", databasesNames)));
                    } else {
                        send(new Message(MessageAction.GET_DATABASES_RESPONSE, ""));
                    }
                    break;

                case MessageAction.CLOSE_CONNECTION:
                default:
                    server.Shutdown(SocketShutdown.Both);
                    server.Close();

                    Environment.Exit(0);
                    break;
            }
        }

        public static XmlNode createXmlNodeWithAttributes(string nodeName, Dictionary<string, string> nodeAttributes, string nodeValue = null) {
            XmlNode node = catalog.CreateElement(nodeName);
            node.InnerText = nodeValue;
            XmlAttribute attribute;
            foreach (var item in nodeAttributes) {
                attribute = catalog.CreateAttribute(item.Key);
                attribute.Value = item.Value;
                node.Attributes.Append(attribute);
            }

            return node;
        }

        public static void appendXmlNodeTo(XmlNode node, string parentNamePath) {
            catalog.Load("../../../Catalog.xml");

            XmlNode parentNode = catalog.SelectSingleNode(parentNamePath);
            if (parentNode != null) {
                parentNode.AppendChild(node);
            }

            catalog.Save("../../../Catalog.xml");
        }

        public static bool xmlNodeExists(string nodeNamePath) {
            catalog.Load("../../../Catalog.xml");

            XmlNode node = catalog.SelectSingleNode(nodeNamePath);
            return node != null;
        }

        public static void removeXmlNodeFrom(string nodeNamePath, string parentNamePath) {
            catalog.Load("../../../Catalog.xml");

            XmlNode parentNode = catalog.SelectSingleNode(parentNamePath);
            if (parentNode != null) {
                XmlNode node = catalog.SelectSingleNode(parentNamePath + "/" + nodeNamePath);
                if (node != null) {
                    parentNode.RemoveChild(node);
                }
            }

            catalog.Save("../../../Catalog.xml");
        }

        public static string getXmlNodeValue(string nodeNamePath) {
            catalog.Load("../../../Catalog.xml");

            XmlNode node = catalog.SelectSingleNode(nodeNamePath);
            return node.InnerText;
        }

        public static string getXmlNodeAttributeValue(string nodeNamePath, string attributeName) {
            catalog.Load("../../../Catalog.xml");

            XmlNode node = catalog.SelectSingleNode(nodeNamePath);
            return node.Attributes[attributeName].InnerText;
        }

        public static List<string> getXmlNodeChildrenValues(string nodeNamePath) {
            List<string> values = new List<string>();
            XmlNode parent = catalog.SelectSingleNode(nodeNamePath);
            if (parent != null) {
                XmlNodeList children = parent.ChildNodes;
                foreach (XmlNode child in children) {
                    values.Add(child.InnerText);
                }
            }

            return values;
        }

        public static void createDBDirectory(string dbName) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases");
            if (Directory.Exists(dbDirectoryPath)) {
                Directory.CreateDirectory(Path.Combine(dbDirectoryPath, dbName));
            }
        }

        public static void removeDBDirectory(string dbName) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                Directory.Delete(dbDirectoryPath, true);
            }
        }

        public static FileStream createFile(string dbName, string fileName, string extension) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string filePath = Path.Combine(dbDirectoryPath, fileName + extension);
                if (!File.Exists(filePath)) {
                    return File.Create(filePath);
                }
            }

            return null;
        }

        public static void removeFile(string dbName, string fileName, string extension) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string filePath = Path.Combine(dbDirectoryPath, fileName + extension);
                if (File.Exists(filePath)) {
                    File.Delete(filePath);
                }
            }
        }

        public static void appendKVInTableFile(string dbName, string tableName, string key, List<string> values) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string tableFilePath = Path.Combine(dbDirectoryPath, tableName + ".kv");
                if (File.Exists(tableFilePath)) {
                    string value = string.Join("#", values);
                    File.AppendAllLines(tableFilePath, new string[] { key + "|" + value });
                }
            }
        }

        public static void appendINDInIndexFile(string dbName, string indexName, string key, string value) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string tableFilePath = Path.Combine(dbDirectoryPath, indexName + ".ind");
                if (File.Exists(tableFilePath)) {
                    File.AppendAllLines(tableFilePath, new string[] { key + "|" + value });
                }
            }
        }

        public static void clearKVTableFile(string dbName, string tableName) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string tableFilePath = Path.Combine(dbDirectoryPath, tableName + ".kv");
                if (File.Exists(tableFilePath)) {
                    File.WriteAllText(tableFilePath, String.Empty);
                }
            }
        }

        public static void removeKVFromTableFile(string dbName, string tableName, string key) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string tableFilePath = Path.Combine(dbDirectoryPath, tableName + ".kv");
                if (File.Exists(tableFilePath)) {
                    List<string> lines = File.ReadAllLines(tableFilePath).ToList();
                    List<string> newlines = new List<string>();
                    if (lines.Count > 0) {
                        int count = 0;
                        foreach (string line in lines) {
                            if (line.Split("|")[0] != key) {
                                newlines.Add(line);
                            } else {
                                count++;
                            }
                        }
                        if (count > 0) {
                            File.WriteAllLines(tableFilePath, newlines);
                            return;
                        } else {
                            send(new Message(MessageAction.ERROR, "Nu exista valoarea in tabel."));
                        }     
                    } else {
                        send(new Message(MessageAction.ERROR, "Nu exista date in fisier."));
                    }
                }
            }
            
            return;
        }

        public static List<string> getValuesByKey(string dbName, string tableName, string key) {
            string dbDirectoryPath = Path.Combine(workingPath, "Databases", dbName);
            if (Directory.Exists(dbDirectoryPath)) {
                string tableFilePath = Path.Combine(dbDirectoryPath, tableName + ".kv");
                if (File.Exists(tableFilePath)) {
                    List<string> lines = File.ReadAllLines(tableFilePath).ToList();
                    if (lines.Count > 0) {
                        foreach (string line in lines) {
                            if (line.StartsWith(key)) {
                                string[] keyValue = line.Split("|");
                                List<string> values = keyValue[1].Split("#").ToList();

                                return values;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static void executeQuery(SQLQuery sqlQuery) {
            catalog.Load("../../../Catalog.xml");

            XmlNode databasesNode = catalog.SelectSingleNode(@"//Databases");
            if (databasesNode != null) {
                switch (sqlQuery.type) {
                    case SQLQueryType.CREATE_DATABASE:
                        if (xmlNodeExists(@"//Databases/Database[@databaseName='" + sqlQuery.CREATE_DATABASE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Baza de date '" + sqlQuery.CREATE_DATABASE_NAME + "' exista deja."));
                            return;
                        }

                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("Database", new Dictionary<string, string> {
                                { "databaseName", sqlQuery.CREATE_DATABASE_NAME }
                            }),
                            @"//Databases"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("Tables", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName='" + sqlQuery.CREATE_DATABASE_NAME + "']"
                        );

                        createDBDirectory(sqlQuery.CREATE_DATABASE_NAME);

                        currentDatabase = sqlQuery.CREATE_DATABASE_NAME;
                        send(new Message(MessageAction.SELECT_DATABASE, sqlQuery.CREATE_DATABASE_NAME));
                        break;

                    case SQLQueryType.CREATE_TABLE:
                        if (currentDatabase == null) {
                            send(new Message(MessageAction.ERROR, "Nicio baza de date selectata."));
                            return;
                        }
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']")) {
                            send(new Message(MessageAction.ERROR, "Nu exista baze de date."));
                            return;
                        }
                        if (xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.CREATE_TABLE_NAME + "' exista deja."));
                            return;
                        }

                        foreach (TableAttribute attribute in sqlQuery.CREATE_TABLE_ATTRIBUTES) {
                            if (attribute.isForeignKey) {
                                if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + attribute.foreignKeyTableReferenceName + "']")) {
                                    send(new Message(MessageAction.ERROR, "Tabela de referinta '" + attribute.foreignKeyTableReferenceName + "' nu exista."));
                                    return;
                                }

                                List<string> referenceTablePKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + attribute.foreignKeyTableReferenceName + "']/PrimaryKeys");
                                if (!referenceTablePKs.Contains(attribute.foreignKeyTableReferenceKey)) {
                                    send(new Message(MessageAction.ERROR, "Cheia '" + attribute.foreignKeyTableReferenceKey + "' din tabela de referinta '" + attribute.foreignKeyTableReferenceName + "' nu exista."));
                                    return;
                                }
                            }
                        }

                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("Table", new Dictionary<string, string> {
                                { "tableName", sqlQuery.CREATE_TABLE_NAME },
                                { "fileName", sqlQuery.CREATE_TABLE_NAME + ".b" }
                            }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("Structure", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("PrimaryKeys", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("ForeignKeys", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("UniqueKeys", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("IndexFiles", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']"
                        );
                        foreach (TableAttribute attribute in sqlQuery.CREATE_TABLE_ATTRIBUTES) {
                            appendXmlNodeTo(
                                createXmlNodeWithAttributes("Attribute", new Dictionary<string, string> {
                                    { "name", attribute.name },
                                    { "type", attribute.type.ToString().ToLower() },
                                    { "length", attribute.length.ToString() },
                                    { "isnull", attribute.isNull.ToString().ToLower() }
                                }),
                                @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/Structure"
                            );

                            if (attribute.isPrimaryKey) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("PrimaryKeyAttribute", new Dictionary<string, string> { }, attribute.name),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/PrimaryKeys"
                                );
                            }
                            if (attribute.isForeignKey) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("ForeignKey", new Dictionary<string, string> {
                                        { "name", attribute.name }
                                    }),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/ForeignKeys"
                                );
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("ForeignKeyAttribute", new Dictionary<string, string> { }, attribute.name),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/ForeignKeys/ForeignKey[@name='" + attribute.name + "']"
                                );
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("References", new Dictionary<string, string> {
                                        { "tableName", attribute.foreignKeyTableReferenceName }
                                    }),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/ForeignKeys/ForeignKey[@name='" + attribute.name + "']"
                                );
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("ReferenceTable", new Dictionary<string, string> { }, attribute.foreignKeyTableReferenceName),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/ForeignKeys/ForeignKey[@name='" + attribute.name + "']/References[@tableName='" + attribute.foreignKeyTableReferenceName + "']"
                                );
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("ReferenceAttribute", new Dictionary<string, string> { }, attribute.foreignKeyTableReferenceKey),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/ForeignKeys/ForeignKey[@name='" + attribute.name + "']/References[@tableName='" + attribute.foreignKeyTableReferenceName + "']"
                                );
                                // TODO: Can be more foreign attributes and referenced table attributes.
                            }
                            if (attribute.isUnique) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("UniqueKeyAttribute", new Dictionary<string, string> { }, attribute.name),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/UniqueKeys"
                                );
                            }
                        }

                        FileStream createTableKV = createFile(currentDatabase, sqlQuery.CREATE_TABLE_NAME, ".kv");
                        if (createTableKV != null) {
                            createTableKV.Dispose();
                        }

                        send(new Message(MessageAction.SUCCESS, "Tabela '" + sqlQuery.CREATE_TABLE_NAME + "' creata cu succes!"));
                        break;

                    case SQLQueryType.CREATE_INDEX:
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.CREATE_TABLE_NAME + "' nu exista."));
                            return;
                        }
                        if (xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexName='" + sqlQuery.CREATE_INDEX_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Indexul '" + sqlQuery.CREATE_INDEX_NAME + "' exista deja."));
                            return;
                        }

                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("IndexFile", new Dictionary<string, string> {
                                { "indexFileName", sqlQuery.CREATE_INDEX_NAME + ".b" }
                            }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("IndexAttributes", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexFileName='" + sqlQuery.CREATE_INDEX_NAME + ".b" + "']"
                        );

                        List<string> createIndexPKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/PrimaryKeys");
                        foreach (string indexAttribute in sqlQuery.CREATE_INDEX_TABLE_FIELDS) {
                            if (!createIndexPKs.Contains(indexAttribute)) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("IndexAttribute", new Dictionary<string, string> { }, indexAttribute),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexFileName='" + sqlQuery.CREATE_INDEX_NAME + ".b" + "']/IndexAttributes"
                                );
                            }
                        } // TODO: Maybe check here for table PKs, don't use as index.

                        
                        FileStream createIndexIND = createFile(currentDatabase, sqlQuery.CREATE_INDEX_NAME, ".ind"); ;
                        if (createIndexIND != null) {
                            createIndexIND.Dispose();
                        }

                        send(new Message(MessageAction.SUCCESS, "Index '" + sqlQuery.CREATE_INDEX_NAME + "' creat cu succes!"));
                        break;

                    case SQLQueryType.DROP_DATABASE:
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName='" + sqlQuery.DROP_DATABASE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Baza de date '" + sqlQuery.DROP_DATABASE_NAME + "' nu exista."));
                            return;
                        }

                        removeXmlNodeFrom(
                            @"Database[@databaseName='" + sqlQuery.DROP_DATABASE_NAME + "']",
                            @"//Databases"
                        );

                        removeDBDirectory(sqlQuery.DROP_DATABASE_NAME);

                        currentDatabase = null;
                        send(new Message(MessageAction.SUCCESS, "Baza de date '" + sqlQuery.DROP_DATABASE_NAME + "' stearsa cu succes!"));
                        break;

                    case SQLQueryType.DROP_TABLE:
                        if (currentDatabase == null) {
                            send(new Message(MessageAction.ERROR, "Nicio baza de date selectata."));
                            return;
                        }
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DROP_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.DROP_TABLE_NAME + "' nu exista."));
                            return;
                        }

                        removeXmlNodeFrom(
                            @"Table[@tableName='" + sqlQuery.DROP_TABLE_NAME + "']",
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables"
                        );

                        removeFile(currentDatabase, sqlQuery.DROP_TABLE_NAME, ".kv");

                        send(new Message(MessageAction.SUCCESS, "Tabela '" + sqlQuery.DROP_TABLE_NAME + "' stearsa cu succes!"));
                        break;

                    case SQLQueryType.USE:
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName='" + sqlQuery.USE_DATABASE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Baza de date '" + sqlQuery.USE_DATABASE_NAME + "' nu exista."));
                            return;
                        }

                        currentDatabase = sqlQuery.USE_DATABASE_NAME;
                        send(new Message(MessageAction.SELECT_DATABASE, sqlQuery.USE_DATABASE_NAME));
                        break;

                    case SQLQueryType.INSERT:
                        if (currentDatabase == null) {
                            send(new Message(MessageAction.ERROR, "Nicio baza de date selectata."));
                            return;
                        }
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.INSERT_TABLE_NAME + "' nu exista."));
                            return;
                        }

                        List<string> keyConcat = new List<string>();
                        List<string> values = new List<string>();
                        List<string> insertPKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']/PrimaryKeys");
                        // string indexKeys = getXmlNodeValue(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']/IndexFiles/IndexFile[@indexFileName='" + sqlQuery.inse + "']/IndexAttributes");

                        foreach (KeyValuePair<string, string> attribute in sqlQuery.INSERT_TABLE_ATTRIBUTES_VALUES) {
                            if (insertPKs.Contains(attribute.Key)) {
                                keyConcat.Add(attribute.Value);
                            }
                            if (!keyConcat.Contains(attribute.Value)) {
                                values.Add(attribute.Value);
                            }
                        }
                        
                        appendKVInTableFile(currentDatabase, sqlQuery.INSERT_TABLE_NAME, string.Join(String.Empty, keyConcat), values);

                        send(new Message(MessageAction.SUCCESS, "Datele inserate cu succes in tabela '" + sqlQuery.INSERT_TABLE_NAME + "'!"));
                        break;
                        
                    case SQLQueryType.DELETE:
                        if (currentDatabase == null) {
                            send(new Message(MessageAction.ERROR, "Nicio baza de date selectata."));
                            return;
                        }
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.DELETE_TABLE_NAME + "' nu exista."));
                            return;
                        }

                        //TODO: Verify if data exists in the kv file. And if condition name value exists. Delete all if primary key match the condition.
                        List<string> kvIndexConcat = new List<string>();
                        List<string> deletePKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/PrimaryKeys");
                        
                        foreach (WhereCondition condition in sqlQuery.DELETE_TABLE_CONDITIONS) {
                            if (deletePKs.Contains(condition.name)) {
                                kvIndexConcat.Add(condition.value);
                            }
                        }

                        removeKVFromTableFile(currentDatabase, sqlQuery.DELETE_TABLE_NAME, string.Join(String.Empty, kvIndexConcat));
                        send(new Message(MessageAction.SUCCESS, "Datele sterse cu succes din tabela '" + sqlQuery.DELETE_TABLE_NAME + "'!"));
                        break;

                    default:
                        break;
                }
            } else {
                send(new Message(MessageAction.ERROR, "Nu exista 'Databases' in 'Catalog.xml'."));
            }

            catalog.Save("../../../Catalog.xml");
        }

        public static Dictionary<TableAttributeType, int> parseAttributeTypeLength(string str) {
            if (str.Contains("(") && str.Contains(")")) {
                int start = 0;
                int end = str.IndexOf("(");
                string type = str.Substring(start, end).ToLower();
                TableAttributeType attributeType;
                if (type == "int") {
                    attributeType = TableAttributeType.INT;
                } else {
                    attributeType = TableAttributeType.VARCHAR;
                }

                start = str.IndexOf("(") + 1;
                end = str.IndexOf(")", start);
                int length = int.Parse(str.Substring(start, end - start));

                return new Dictionary<TableAttributeType, int> { { attributeType, length } };
            } else {
                if (str.ToLower() == "int") {
                    return new Dictionary<TableAttributeType, int> { { TableAttributeType.INT, 11 } };
                } else {
                    return new Dictionary<TableAttributeType, int> { { TableAttributeType.VARCHAR, 255 } };
                }
            }
        }

        public static SQLQuery parseStatement(string statement) {
            statement = statement.Replace(";", String.Empty);

            string pattern = @"(?<=\().*(?=\))";
            List<string> matches = Regex.Matches(statement, pattern).Cast<Match>().Select(match => match.Value).ToList();
            string replacedStatement = Regex.Replace(statement, pattern, "%");

            string[] args = replacedStatement.Split(" ");

            string replaced;
            SQLQuery sqlQuery;
            switch (args[0].ToLower()) {
                case "create":
                    switch (args[1].ToLower()) {
                        case "database":
                            sqlQuery = new SQLQuery(SQLQueryType.CREATE_DATABASE);
                            sqlQuery.CREATE_DATABASE_NAME = args[2];
                            break;

                        case "table":
                            string[] structure = matches[0].Split(", "); // Watch out for the table attributes to be split by ", ".

                            List<TableAttribute> tableAttributes = new List<TableAttribute>();
                            foreach (string item in structure) {
                                string[] attributeArgs = item.Split(" ");

                                if (item.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)) {
                                    // Composite primary key
                                    int start = attributeArgs[2].IndexOf("(") + 1;
                                    int end = attributeArgs[2].IndexOf(")", start);
                                    string str = attributeArgs[2].Substring(start, end - start);
                                    string[] keys = str.Split(",", StringSplitOptions.TrimEntries);
                                    
                                    foreach (TableAttribute attribute in tableAttributes) {
                                        if (keys.Contains(attribute.name)) {
                                            attribute.isPrimaryKey = true;
                                        }
                                    }
                                } else {
                                    KeyValuePair<TableAttributeType, int> attributeTypeLength = parseAttributeTypeLength(attributeArgs[1]).FirstOrDefault();

                                    if (item.Contains("REFERENCES", StringComparison.OrdinalIgnoreCase)) {
                                        // Foreign key
                                        int start = attributeArgs[4].IndexOf("(") + 1;
                                        int end = attributeArgs[4].IndexOf(")", start);
                                        string key = attributeArgs[4].Substring(start, end - start);
                                        
                                        tableAttributes.Add(new TableAttribute(
                                            attributeArgs[0],
                                            attributeTypeLength.Key,
                                            attributeTypeLength.Value,
                                            item.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ? false : true,
                                            item.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ? true : false,
                                            false,
                                            true, 
                                            attributeArgs[3],
                                            key
                                        ));
                                    } else {
                                        if (item.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)) {
                                            // Primary key
                                            tableAttributes.Add(new TableAttribute(
                                                attributeArgs[0],
                                                attributeTypeLength.Key,
                                                attributeTypeLength.Value,
                                                item.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ? false : true,
                                                item.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ? true : false,
                                                true
                                            ));
                                        } else {
                                            // Normal key
                                            tableAttributes.Add(new TableAttribute(
                                                attributeArgs[0],
                                                attributeTypeLength.Key,
                                                attributeTypeLength.Value,
                                                item.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase) ? false : true,
                                                item.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ? true : false
                                            ));
                                        }
                                    }
                                }
                            }

                            sqlQuery = new SQLQuery(SQLQueryType.CREATE_TABLE);
                            sqlQuery.CREATE_TABLE_NAME = args[2];
                            sqlQuery.CREATE_TABLE_ATTRIBUTES = tableAttributes;
                            break;

                        case "index":
                            List<string> fields = matches[0].Split(",", StringSplitOptions.TrimEntries).ToList();

                            sqlQuery = new SQLQuery(SQLQueryType.CREATE_INDEX);
                            sqlQuery.CREATE_INDEX_NAME = args[2];
                            sqlQuery.CREATE_INDEX_TABLE_NAME = args[4];
                            sqlQuery.CREATE_INDEX_TABLE_FIELDS = fields;
                            break;

                        default:
                            sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                            sqlQuery.error = "SQL query invalid.";
                            break;
                    }
                    break;

                case "drop":
                    switch (args[1].ToLower()) {
                        case "database":
                            sqlQuery = new SQLQuery(SQLQueryType.DROP_DATABASE);
                            sqlQuery.DROP_DATABASE_NAME = args[2];
                            break;

                        case "table":
                            sqlQuery = new SQLQuery(SQLQueryType.DROP_TABLE);
                            sqlQuery.DROP_TABLE_NAME = args[2];
                            break;

                        default:
                            sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                            sqlQuery.error = "SQL query invalid.";
                            break;
                    }
                    break;

                case "use":
                    sqlQuery = new SQLQuery(SQLQueryType.USE);
                    sqlQuery.USE_DATABASE_NAME = args[1];
                    break;

                case "insert":
                    if (args[1].Contains("INTO", StringComparison.OrdinalIgnoreCase)) {
                        pattern = @"(?<=\()(.*?)(?=\))";
                        matches = Regex.Matches(statement, pattern).Cast<Match>().Select(match => match.Value).ToList();
                       
                        List<KeyValuePair<string, string>> attributesValues = new List<KeyValuePair<string, string>>();
                        string[] attributes = matches[0].Split(",", StringSplitOptions.TrimEntries);
                        string[] values = matches[1].Split(",", StringSplitOptions.TrimEntries);
                        for (int i = 0; i < attributes.Length; i++) {
                            attributesValues.Add(new KeyValuePair<string, string>(attributes[i], values[i].Replace("\'", String.Empty)));
                        }

                        sqlQuery = new SQLQuery(SQLQueryType.INSERT);
                        sqlQuery.INSERT_TABLE_NAME = args[2];
                        sqlQuery.INSERT_TABLE_ATTRIBUTES_VALUES = attributesValues;
                    } else {
                        sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                        sqlQuery.error = "SQL query invalid.";
                    }
                    break;

                case "delete":
                    if (args[1].Contains("FROM", StringComparison.OrdinalIgnoreCase) && args[3].Contains("WHERE", StringComparison.OrdinalIgnoreCase)) {
                        pattern = @"(?<=WHERE ).*";
                        matches = Regex.Matches(statement, pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Value).ToList();
                        if (matches.Count > 0) {
                            List<WhereCondition> whereConditions = new List<WhereCondition>();
                            string[] conditions = matches[0].Split("AND", StringSplitOptions.TrimEntries);
                            foreach (string condition in conditions) {
                                string[] conditionArgs = condition.Split(" "); // Watch out for the conditions to be split by " ".
                                switch (conditionArgs[1]) {
                                    case "=":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.EQUAL, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    case "!=":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.NOT_EQUAL, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    case "<":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.LESS_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    case "<=":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.LESS_OR_EQUAL_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    case ">":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.GREATER_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    case ">=":
                                        whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.GREATER_OR_EQUAL_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                        break;
                                    default:
                                        break;
                                }
                            }

                            sqlQuery = new SQLQuery(SQLQueryType.DELETE);
                            sqlQuery.DELETE_TABLE_NAME = args[2];
                            sqlQuery.DELETE_TABLE_CONDITIONS = whereConditions;
                        } else {
                            sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                            sqlQuery.error = "SQL query invalid.";
                        }
                    } else {
                        sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                        sqlQuery.error = "SQL query invalid.";
                    }
                    break;

                default:
                    sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                    sqlQuery.error = "SQL query invalid.";

                    break;
            }

            return sqlQuery;
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

        public static void clientList() {
            Console.Clear();
            Console.WriteLine("Client ({0}) conectat.", server.RemoteEndPoint.ToString());
            Console.WriteLine();
        }
    }
}