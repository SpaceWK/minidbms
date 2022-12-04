using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;
using System.Diagnostics.Metrics;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static MongoDB.Driver.WriteConcern;

namespace Server {
    public class Program {
        public static Socket server;
        public static XmlDocument catalog = new XmlDocument();
        public static MongoDBService mongoDBService;

        public static string currentDatabase;

        public static string workingPath;

        public static void Main(string[] args) {
            workingPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;

            string dbDirectoryPath = Path.Combine(workingPath, "Databases");
            if (!Directory.Exists(dbDirectoryPath)) {
                Directory.CreateDirectory(Path.Combine(dbDirectoryPath, "Databases"));
            }

            mongoDBService = new MongoDBService();

            IPAddress ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, 11000);

            try {
                Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(ipEndPoint);
                listener.Listen(100);

                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("Se asteapta o conexiune...");
                Console.ResetColor();
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
            DateTime dateTime = DateTime.Now;

            switch (response.action) {
                case MessageAction.SQL_QUERY_REQUEST:
                    SQLQuery sqlQuery = parseStatement(response.value);
                    if (sqlQuery != null && sqlQuery.error == null) {
                        Console.WriteLine("[" + dateTime.ToString("HH:MM:ss") + "] Executa query: {0}", response.value);
                        executeQuery(sqlQuery);
                    } else {
                        send(new Message(MessageAction.ERROR, sqlQuery.error));
                    }
                    break;

                case MessageAction.GET_DATABASES_REQUEST:
                    catalog.Load("../../../Catalog.xml");
                    Console.WriteLine("[" + dateTime.ToString("HH:MM:ss") + "] Lista baze de date.");

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

                case MessageAction.GET_TABLES_REQUEST:
                    catalog.Load("../../../Catalog.xml");
                    Console.WriteLine("[" + dateTime.ToString("HH:MM:ss") + "] Lista tabele din baza de date: {0}", response.value);

                    XmlNodeList tablesNodes = catalog.SelectNodes(@"//Databases/Database[@databaseName='" + response.value + "']/Tables/Table");
                    if (tablesNodes != null && tablesNodes.Count > 0) {
                        List<string> tableNames = new List<string>();
                        foreach (XmlNode item in tablesNodes) {
                            tableNames.Add(item.Attributes["tableName"].Value);
                        }

                        send(new Message(MessageAction.GET_TABLES_RESPONSE, String.Join(",", tableNames)));
                    } else {
                        send(new Message(MessageAction.GET_TABLES_RESPONSE, ""));
                    }
                    break;

                case MessageAction.GET_TABLE_DATA_REQUEST:
                    catalog.Load("../../../Catalog.xml");
                    Console.WriteLine("[" + dateTime.ToString("HH:MM:ss") + "] Lista date din tabela: {0}", response.value);

                    XmlNodeList tableFieldsNodes = catalog.SelectNodes(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + response.value + "']/Structure/Attribute");
                    if (tableFieldsNodes != null && tableFieldsNodes.Count > 0) {
                        List<string> tableFields = new List<string>();
                        foreach (XmlNode item in tableFieldsNodes) {
                            tableFields.Add(item.Attributes["name"].Value);
                        }

                        string message = "";
                        message += string.Join(",", tableFields);
                        message += ":";

                        List<Record> records = mongoDBService.getAll(currentDatabase, response.value);
                        List<string> data = records.Select(record => string.Join("#", record.key, record.value)).ToList();
                        message += string.Join("^", data);

                        send(new Message(MessageAction.GET_TABLE_DATA_RESPONSE, message));
                    } else {
                        send(new Message(MessageAction.ERROR, "Tabela '" + response.value + "' nu exista."));
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
            catalog.Load("../../../Catalog.xml");

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

        public static List<string> getXmlNodeChildrenAttributeValues(string nodeNamePath, string attributeName) {
            catalog.Load("../../../Catalog.xml");

            List <string> values = new List<string>();
            XmlNode parent = catalog.SelectSingleNode(nodeNamePath);
            if (parent != null) {
                XmlNodeList children = parent.ChildNodes;
                foreach (XmlNode child in children) {
                    values.Add(child.Attributes[attributeName].Value);
                }
            }

            return values;
        }

        public static List<ForeignKey> getXmlTableForeignKeys(string tableName) {
            List<ForeignKey> foreignKeys = new List<ForeignKey>();
            List<string> tableFKs = getXmlNodeChildrenAttributeValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + tableName + "']/ForeignKeys", "name");
            foreach (string fkName in tableFKs) {
                string fkAttributeName = getXmlNodeValue(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + tableName + "']/ForeignKeys/ForeignKey[@name='" + fkName + "']/ForeignKeyAttribute");
                List<string> fkReferencedTableAndKey = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + tableName + "']/ForeignKeys/ForeignKey[@name='" + fkName + "']/References");

                foreignKeys.Add(new ForeignKey(fkAttributeName, fkReferencedTableAndKey[0], fkReferencedTableAndKey[1]));
            }
            
            return foreignKeys;
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

        public static void insertDataIntoIdxCollection(string collection, string key, string value) {
            List<Record> records = mongoDBService.getAll(currentDatabase, collection);
            if (records.Count() > 0) {
                int counter = 0;
                foreach (Record record in records) {
                    if (key == record.key) {
                        counter++;
                    }
                }
                if (counter < 1) {
                    mongoDBService.insert(
                        currentDatabase,
                        collection,
                        new Record(key, value)
                    );
                } else {
                    send(new Message(MessageAction.ERROR, "Exista inregistrarea cu cheia " + key + " in tabela '" + collection + "'!"));
                    return;
                }
            } else {
                mongoDBService.insert(
                    currentDatabase,
                    collection,
                    new Record(key, value)
                );
            }
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

                        mongoDBService.createDatabase(sqlQuery.CREATE_DATABASE_NAME);

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

                                mongoDBService.createCollection(currentDatabase, "idx_" + sqlQuery.CREATE_TABLE_NAME + "_" + attribute.name);
                            }
                            if (attribute.isUnique) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("UniqueKeyAttribute", new Dictionary<string, string> { }, attribute.name),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_TABLE_NAME + "']/UniqueKeys"
                                );

                                mongoDBService.createCollection(currentDatabase, "idx_" + sqlQuery.CREATE_TABLE_NAME + "_" + attribute.name);
                            }
                        }

                        mongoDBService.createCollection(currentDatabase, sqlQuery.CREATE_TABLE_NAME);

                        send(new Message(MessageAction.SUCCESS, "Tabela '" + sqlQuery.CREATE_TABLE_NAME + "' creata cu succes!"));
                        break;

                    case SQLQueryType.CREATE_INDEX:
                        // !!! Create index name should match the following structure when executed: idx_TABLE-NAME_KEYS-NAMES

                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.CREATE_INDEX_TABLE_NAME + "' nu exista."));
                            return;
                        }
                        if (xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexName='" + sqlQuery.CREATE_INDEX_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Indexul '" + sqlQuery.CREATE_INDEX_NAME + "' exista deja."));
                            return;
                        }

                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("IndexFile", new Dictionary<string, string> {
                                { "indexName", sqlQuery.CREATE_INDEX_NAME }
                            }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles"
                        );
                        appendXmlNodeTo(
                            createXmlNodeWithAttributes("IndexAttributes", new Dictionary<string, string> { }),
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexName='" + sqlQuery.CREATE_INDEX_NAME + "']"
                        );

                        List<string> createIndexPKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/PrimaryKeys");
                        foreach (string indexAttribute in sqlQuery.CREATE_INDEX_TABLE_FIELDS) {
                            if (!createIndexPKs.Contains(indexAttribute)) {
                                appendXmlNodeTo(
                                    createXmlNodeWithAttributes("IndexAttribute", new Dictionary<string, string> { }, indexAttribute),
                                    @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexName='" + sqlQuery.CREATE_INDEX_NAME + "']/IndexAttributes"
                                );
                            } else {
                                send(new Message(MessageAction.ERROR, "Nu se poate creea index pe cheia primara '" + indexAttribute + "'."));
                                return;
                            }
                        }

                        bool createIndexCollection = mongoDBService.createCollection(currentDatabase, sqlQuery.CREATE_INDEX_NAME);
                        if (!createIndexCollection) {
                            send(new Message(MessageAction.ERROR, "Indexul '" + sqlQuery.CREATE_INDEX_NAME + "' nu a fost creat."));
                            return;
                        }

                        List<Record> records = mongoDBService.getAll(currentDatabase, sqlQuery.CREATE_INDEX_TABLE_NAME);
                        if (records.Count() > 0) {
                            List<string> createIndexIDXs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/IndexFiles/IndexFile[@indexName='" + sqlQuery.CREATE_INDEX_NAME + "']/IndexAttributes");
                            
                            if (createIndexIDXs.Count() > 0) {
                                List<string> createIndexAttributes = getXmlNodeChildrenAttributeValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.CREATE_INDEX_TABLE_NAME + "']/Structure", "name");
                                createIndexAttributes.RemoveAll(name => createIndexPKs.Contains(name));

                                foreach (Record record in records) {
                                    string indexAttributeValue = "";
                                    foreach (string indexAttribute in createIndexIDXs) {
                                        indexAttributeValue = string.Concat(
                                            indexAttributeValue,
                                            record.getKeyValue(indexAttribute, createIndexAttributes)
                                        );
                                    }

                                    mongoDBService.insert(
                                        currentDatabase,
                                        sqlQuery.CREATE_INDEX_NAME,
                                        new Record(indexAttributeValue, record.key)
                                    );
                                }
                            }
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

                        mongoDBService.removeDatabase(sqlQuery.DROP_DATABASE_NAME);

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
                        List<string> dropTableAllTableNames = getXmlNodeChildrenAttributeValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables", "tableName");
                        dropTableAllTableNames.Remove(sqlQuery.DROP_TABLE_NAME);
                        if (dropTableAllTableNames.Count() > 0) {
                            foreach (string tableName in dropTableAllTableNames) {
                                List<string> dropTableReferenceTableNames = getXmlTableForeignKeys(tableName).Select(fk => fk.referencedTableName).ToList();
                                if (dropTableReferenceTableNames.Count() > 0 && dropTableReferenceTableNames.Contains(sqlQuery.DROP_TABLE_NAME)) {
                                    send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.DROP_TABLE_NAME + "' nu poate fi eliminata, deoarece este referentiata de o cheie straina."));
                                    return;
                                }
                            }
                        }

                        removeXmlNodeFrom(
                            @"Table[@tableName='" + sqlQuery.DROP_TABLE_NAME + "']",
                            @"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables"
                        );

                        mongoDBService.removeCollection(currentDatabase, sqlQuery.DROP_TABLE_NAME);

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

                        List<KeyValuePair<string,string>> insertUniqueKeys = new List<KeyValuePair<string, string>>();
                        List<KeyValuePair<string, string>> insertIndexKeys = new List<KeyValuePair<string, string>>();
                        List<KeyValuePair<string, string>> insertForeignKeys = new List<KeyValuePair<string, string>>();

                        string insertPrimaryKey = "";
                        List<string> insertValues = new List<string>();

                        List<string> insertPKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']/PrimaryKeys");
                        List<string> insertUQs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']/UniqueKeys");
                        List<string> insertIndexes = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.INSERT_TABLE_NAME + "']/IndexFiles");
                        List<ForeignKey> insertFKs = getXmlTableForeignKeys(sqlQuery.INSERT_TABLE_NAME);
                        
                        if (insertUQs.Count() > 0 || insertPKs.Count() > 0 || insertIndexes.Count() > 0 || insertFKs.Count() > 0) {
                            foreach (KeyValuePair<string, string> attribute in sqlQuery.INSERT_TABLE_ATTRIBUTES_VALUES) {
                                if (insertPKs.Contains(attribute.Key)) {
                                    insertPrimaryKey = string.Concat(insertPrimaryKey, attribute.Value);
                                } else if (insertUQs.Contains(attribute.Key)) {
                                    insertUniqueKeys.Add(attribute);
                                } else if (insertIndexes.Contains(attribute.Key)) {
                                    insertIndexKeys.Add(attribute);
                                } else {
                                    foreach (ForeignKey foreignKey in insertFKs) {
                                        if (foreignKey.attribute == attribute.Key) {
                                            insertForeignKeys.Add(attribute);
                                        }
                                    }
                                }

                                if (!insertPrimaryKey.Contains(attribute.Value)) {
                                    insertValues.Add(attribute.Value);
                                }
                            }

                            foreach (KeyValuePair<string, string> key in insertUniqueKeys) {
                                insertDataIntoIdxCollection(
                                    "idx_" + sqlQuery.INSERT_TABLE_NAME + "_" + key.Key,
                                    key.Value,
                                    insertPrimaryKey
                                );
                            }

                            foreach (KeyValuePair<string, string> key in insertIndexKeys) {
                                insertDataIntoIdxCollection(
                                    "idx_" + sqlQuery.INSERT_TABLE_NAME + "_" + key.Key,
                                    key.Value,
                                    insertPrimaryKey
                                );
                            }

                            bool checkReferenceTables = true;
                            foreach (ForeignKey foreignKey in insertFKs) {
                                List<Record> referenceCollectionRecords = mongoDBService.getAll(currentDatabase, foreignKey.referencedTableName);
                                if (referenceCollectionRecords.Count() > 0) {
                                    foreach (Record referenceCollectionRecord in referenceCollectionRecords) {
                                        foreach(KeyValuePair<string, string> key in insertForeignKeys) {
                                            if (foreignKey.referencedTableKey == key.Key && referenceCollectionRecord.key == key.Value) {
                                                insertDataIntoIdxCollection(
                                                    "idx_" + sqlQuery.INSERT_TABLE_NAME + "_" + key.Key,
                                                    key.Value,
                                                    insertPrimaryKey
                                                );
                                            } else {
                                                checkReferenceTables = false;
                                                send(new Message(MessageAction.ERROR, "Nu exista inregistrarea in '" + foreignKey.referencedTableName + "'!"));
                                                break;
                                            }
                                        }
                                    }
                                }
                                else {
                                    send(new Message(MessageAction.ERROR, "Nu exista inregistrarea in '" + foreignKey.referencedTableName + "'!"));
                                    break;
                                }
                            }

                            if (checkReferenceTables == true) {
                                string insertFinalValues = string.Join("#", insertValues);
                                insertDataIntoIdxCollection(
                                    sqlQuery.INSERT_TABLE_NAME,
                                    insertPrimaryKey,
                                    insertFinalValues
                                );
                            }
                        }

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
                        if (sqlQuery.DELETE_TABLE_CONDITIONS != null) {
                            List<string> deleteAllTableNames = getXmlNodeChildrenAttributeValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables", "tableName");
                            deleteAllTableNames.Remove(sqlQuery.DELETE_TABLE_NAME);
                            if (deleteAllTableNames.Count() > 0) {
                                foreach (string tableName in deleteAllTableNames) {
                                    List<string> deleteReferenceTableKeys = getXmlTableForeignKeys(tableName).Select(fk => fk.referencedTableKey).ToList();
                                    if (deleteReferenceTableKeys.Count() > 0) {
                                        foreach (WhereCondition condition in sqlQuery.DELETE_TABLE_CONDITIONS) {
                                            if (deleteReferenceTableKeys.Contains(condition.name)) {
                                                send(new Message(MessageAction.ERROR, "Inregistrarea nu poate fi eliminata, deoarece cheia '" + condition.name + "' este referinta in tabela '" + tableName + "'."));
                                                return;
                                            }
                                        }
                                    }
                                }
                            }

                            string deletePrimaryKey = "";
                            List<string> deletePKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/PrimaryKeys");
                            foreach (WhereCondition condition in sqlQuery.DELETE_TABLE_CONDITIONS) {
                                if (deletePKs.Contains(condition.name)) {
                                    deletePrimaryKey = string.Concat(deletePrimaryKey, condition.value);
                                }
                            }
                            mongoDBService.removeByKey(
                                currentDatabase,
                                sqlQuery.DELETE_TABLE_NAME,
                                deletePrimaryKey
                            );

                            string indexTableName;
                            List<string> deleteIDXCollectionUKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/UniqueKeys");
                            List<string> deleteIDXCollectionIDXs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/IndexFiles");
                            List<string> merged = new List<string>();
                            merged.AddRange(deleteIDXCollectionUKs);
                            merged.AddRange(deleteIDXCollectionIDXs);
                            foreach (string key in merged) {
                                indexTableName = "idx_" + sqlQuery.DELETE_TABLE_NAME + "_" + key;
                                if (mongoDBService.existsCollection(currentDatabase, indexTableName)) {
                                    foreach (WhereCondition condition in sqlQuery.DELETE_TABLE_CONDITIONS) {
                                        mongoDBService.removeAllByValue(
                                            currentDatabase,
                                            indexTableName,
                                            condition.value
                                        );
                                    }
                                }
                            }
                        } else {
                            string indexTableName;
                            List<string> deleteNoCondUKs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/UniqueKeys");
                            foreach (string uniqueKey in deleteNoCondUKs) {
                                indexTableName = "idx_" + sqlQuery.DELETE_TABLE_NAME + "_" + uniqueKey;
                                if (mongoDBService.existsCollection(currentDatabase, indexTableName)) {
                                    mongoDBService.clearCollection(currentDatabase, indexTableName);
                                    }
                                }

                            List<string> deleteNoCondIDXs = getXmlNodeChildrenValues(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.DELETE_TABLE_NAME + "']/IndexFiles");
                            foreach (string indexAttribute in deleteNoCondIDXs) {
                                indexTableName = "idx_" + sqlQuery.DELETE_TABLE_NAME + "_" + indexAttribute;
                                if (mongoDBService.existsCollection(currentDatabase, indexTableName)) {
                                    mongoDBService.clearCollection(currentDatabase, indexTableName);
                                }
                            }

                            mongoDBService.clearCollection(currentDatabase, sqlQuery.DELETE_TABLE_NAME);
                        }

                        send(new Message(MessageAction.SUCCESS, "Datele eliminate cu succes din tabela '" + sqlQuery.DELETE_TABLE_NAME + "'!"));
                        break;

                    case SQLQueryType.SELECT:
                        if (currentDatabase == null) {
                            send(new Message(MessageAction.ERROR, "Nicio baza de date selectata."));
                            return;
                        }
                        if (!xmlNodeExists(@"//Databases/Database[@databaseName = '" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.SELECT_TABLE_NAME + "']")) {
                            send(new Message(MessageAction.ERROR, "Tabela '" + sqlQuery.SELECT_TABLE_NAME + "' nu exista."));
                            return;
                        }

                        List<string> selectAttributes = getXmlNodeChildrenAttributeValues(@"//Databases/Database[@databaseName='" + currentDatabase + "']/Tables/Table[@tableName='" + sqlQuery.SELECT_TABLE_NAME + "']/Structure", "name");
                        bool verifyProjection = false;
                        bool verifySelection = false;
                        foreach (string projection in sqlQuery.SELECT_PROJECTION) {
                            foreach (string attribute in selectAttributes) {
                                if (projection == attribute) {
                                    verifyProjection = true;
                                }
                            }
                            if (verifyProjection == false) {
                                send(new Message(MessageAction.ERROR, "Nu exista cheia in tabela '" + sqlQuery.SELECT_TABLE_NAME));
                                return;
                            }
                            verifyProjection = false;
                        }
                        foreach (WhereCondition selection in sqlQuery.SELECT_SELECTION) {
                            foreach (string attribute in selectAttributes) {
                                if (selection.name == attribute) {
                                    verifySelection = true;
                                }
                            }
                            if (verifySelection == false) {
                                send(new Message(MessageAction.ERROR, "Nu exista cheia in tabela '" + sqlQuery.SELECT_TABLE_NAME));
                                return;
                            }
                            verifySelection = false;
                        }

                        // TODO: Check for the projection & selection (where conditions) fields to exist in the table structure.

                        //

                        send(new Message(MessageAction.SUCCESS, "Select"));
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
                    if (args[1].Contains("FROM", StringComparison.OrdinalIgnoreCase)) {
                        if (args.Length > 3 && args[3].Contains("WHERE", StringComparison.OrdinalIgnoreCase)) {
                            pattern = @"(?<=WHERE ).*";
                            matches = Regex.Matches(statement, pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Value).ToList();
                            if (matches.Count > 0) {
                                List<WhereCondition> whereConditions = new List<WhereCondition>();
                                string[] conditions = Regex.Split(matches[0], " AND ", RegexOptions.IgnoreCase);
                                foreach (string condition in conditions) {
                                    string[] conditionArgs = condition.Split(" "); // Watch out for the conditions to be split by " ".
                                    switch (conditionArgs[1]) {
                                        case "=":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.EQUAL, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;
                                        case "<":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.LESS_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;
                                        case ">":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.GREATER_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;;
                                        default:
                                            break;
                                    }
                                }

                                sqlQuery = new SQLQuery(SQLQueryType.DELETE);
                                sqlQuery.DELETE_TABLE_NAME = args[2];
                                sqlQuery.DELETE_TABLE_CONDITIONS = whereConditions;
                            } else {
                                // TODO: Delete without conditions can be possible.
                                sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                                sqlQuery.error = "SQL query invalid.";
                            }
                        } else {
                            sqlQuery = new SQLQuery(SQLQueryType.DELETE);
                            sqlQuery.DELETE_TABLE_NAME = args[2];
                            sqlQuery.DELETE_TABLE_CONDITIONS = null;
                        }
                    } else {
                        sqlQuery = new SQLQuery(SQLQueryType.ERROR);
                        sqlQuery.error = "SQL query invalid.";
                    }
                    break;

                case "select":
                    pattern = "(?<=SELECT )(.*)(?= FROM)";
                    matches = Regex.Matches(statement, pattern, RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Value).ToList();
                    string selectReplacedStatement = Regex.Replace(statement, pattern, "%");
                    string[] selectReplacedArgs = selectReplacedStatement.Split(" ");

                    if (selectReplacedStatement.Contains("FROM", StringComparison.OrdinalIgnoreCase)) {
                        if (matches.Count() > 0) {
                            List<string> projection = new List<string>();
                            bool isDistinctSelect = false;

                            if (matches[0].Contains("DISTINCT", StringComparison.OrdinalIgnoreCase)) {
                                string distinctPattern = "(?<=DISTINCT )(.*)(?= FROM)";
                                matches = Regex.Matches(statement, distinctPattern, RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Value).ToList();
                                string[] attributes = matches[0].Split(",", StringSplitOptions.TrimEntries); // Watch out for the attributes to be split by ",".
                                foreach (string attribute in attributes) {
                                    projection.Add(attribute);
                                }

                                isDistinctSelect = true;
                            } else {
                                string[] attributes = matches[0].Split(",", StringSplitOptions.TrimEntries); // Watch out for the attributes to be split by ",".
                                foreach (string attribute in attributes) {
                                    projection.Add(attribute);
                                }
                            }

                            string selectPattern = @"(?<=WHERE ).*";
                            List<string> selectMatches = Regex.Matches(selectReplacedStatement, selectPattern, RegexOptions.IgnoreCase).Cast<Match>().Select(match => match.Value).ToList();
                            List<WhereCondition> whereConditions = new List<WhereCondition>();
                            if (selectMatches.Count() > 0) {
                                string[] conditions = Regex.Split(selectMatches[0], " AND ", RegexOptions.IgnoreCase);
                                foreach (string condition in conditions) {
                                    string[] conditionArgs = condition.Split(" "); // Watch out for the conditions to be split by " ".
                                    switch (conditionArgs[1]) {
                                        case "=":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.EQUAL, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;
                                        case "<":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.LESS_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;
                                        case ">":
                                            whereConditions.Add(new WhereCondition(conditionArgs[0], ComparisonOperator.GREATER_THAN, conditionArgs[2].Replace("\'", String.Empty)));
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }

                            sqlQuery = new SQLQuery(SQLQueryType.SELECT);
                            sqlQuery.SELECT_PROJECTION = projection;
                            sqlQuery.SELECT_TABLE_NAME = selectReplacedArgs[3];
                            sqlQuery.SELECT_DISTINCT = isDistinctSelect;
                            sqlQuery.SELECT_SELECTION = whereConditions.Count() > 0 ? whereConditions : null;
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
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Eroare: {0}", message);
            Console.ResetColor();
        }

        public static void clientList() {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("Client ({0}) conectat.", server.RemoteEndPoint.ToString());
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}