using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Server {
    public class MongoDBService {
        public MongoClient mongoClient;

        public MongoDBService() {
            this.mongoClient = new MongoClient("mongodb+srv://minidbms:minidbms@cluster0.wz7z2bg.mongodb.net/test");
        }

        public bool createDatabase(string dbName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                return true;
            }

            return false;
        }

        public bool removeDatabase(string dbName) {
            this.mongoClient.DropDatabase(dbName);
            
            return true;
        }

        public bool createCollection(string dbName, string tableName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                mongoDB.CreateCollection(tableName);
                return true;
            }

            return false;
        }

        public bool existsCollection(string dbName, string tableName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    return true;
                }
            }

            return false;
        }

        public bool removeCollection(string dbName, string tableName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                mongoDB.DropCollection(tableName);
                return true;
            }

            return false;
        }

        public List<Record> getAll(string dbName, string tableName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    return mongoTable.Find(_ => true).ToList();
                }
            }

            return null;
        }

        public Record get(string dbName, string tableName, string key) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    return mongoTable.Find(record => record.key == key).FirstOrDefault();
                }
            }

            return null;
        }

        public Record insert(string dbName, string tableName, Record record) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    mongoTable.InsertOne(record);
                }
            }

            return record;
        }

        public void removeByKey(string dbName, string tableName, string key) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    mongoTable.DeleteOne(record => record.key == key);
                }
            }
        }

        public void removeAllByValue(string dbName, string tableName, string value) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    mongoTable.DeleteMany(record => record.value == value);
                }
            }
        }

        public void clearCollection(string dbName, string tableName) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    mongoTable.DeleteMany(_ => true);
                }
            }
        }
    }
}
