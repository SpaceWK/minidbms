using System;
using System.Collections.Generic;

namespace Server {
    public enum SQLQueryType {
        CREATE_DATABASE,
        CREATE_TABLE,
        CREATE_INDEX,

        DROP_DATABASE,
        DROP_TABLE,

        USE_DATABASE,

        ERROR
    }

    public enum KeyType {
        PRIMARY_KEY,
        FOREIGN_KEY
    }

    public class SQLQuery {
        // Struct
        public SQLQueryType type;
        public string error = null;


        // Query specific
        public string? CREATE_DATABASE_NAME;


        public string? CREATE_TABLE_NAME;
        public Dictionary<string, string>? CREATE_TABLE_ATTRIBUTES;
        public Dictionary<KeyType, List<string>>? CREATE_TABLE_KEYS;


        public string? CREATE_INDEX_NAME;
        public string? CREATE_INDEX_TABLE_NAME;
        public List<string>? CREATE_INDEX_TABLE_FIELDS;


        public string? DROP_DATABASE_NAME;
        public string? DROP_TABLE_NAME;


        public string? USE_DATABASE_NAME;


        public SQLQuery(SQLQueryType type) {
            this.type = type;
        }
    }
}

