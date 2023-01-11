using MongoDB.Bson;
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
            this.mongoClient = new MongoClient("");
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

        public List<Record> getAllByKey(string dbName, string tableName, string key) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    return mongoTable.Find(record => record.key == key).ToList();
                }
            }

            return null;
        }

        public List<Record> getAllByKeyWithCondition(string dbName, string tableName, WhereCondition condition) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    switch (condition.comparison) {
                        case ComparisonOperator.EQUAL:
                            return mongoTable.Find(
                                Builders<Record>.Filter.Eq(record => record.key, condition.value)
                            ).ToList();
                            break;
                        case ComparisonOperator.LESS_THAN:
                            return mongoTable.Aggregate()
                                .AppendStage<Record>("{ $set : { _KeyInt : { $toInt : '$_id' } } }")
                                .Match(Builders<Record>.Filter.Lt("_KeyInt", int.Parse(condition.value)))
                                .AppendStage<object>("{ $unset : '_KeyInt' }")
                                .As<Record>()
                            .ToList();
                            break;
                        case ComparisonOperator.GREATER_THAN:
                            return mongoTable.Aggregate()
                                .AppendStage<Record>("{ $set : { _KeyInt : { $toInt : '$_id' } } }")
                                .Match(Builders<Record>.Filter.Gt("_KeyInt", int.Parse(condition.value)))
                                .AppendStage<object>("{ $unset : '_KeyInt' }")
                                .As<Record>()
                            .ToList();
                            break;
                    }
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

        public void update(string dbName, string tableName, string key, string value) {
            IMongoDatabase mongoDB = this.mongoClient.GetDatabase(dbName);
            if (mongoDB != null) {
                IMongoCollection<Record> mongoTable = mongoDB.GetCollection<Record>(tableName);
                if (mongoTable != null) {
                    var updated = Builders<Record>.Update.Set(record => record.value, value);
                    mongoTable.UpdateOne(record => record.key == key, updated);
                }
            }
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
