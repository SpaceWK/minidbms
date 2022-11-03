using System;
using System.Collections.Generic;

namespace Server {
    public enum SQLQueryType {
        CREATE_DATABASE,
        CREATE_TABLE,
        CREATE_INDEX,

        DROP_DATABASE,
        DROP_TABLE,

        USE,

        INSERT,

        DELETE,

        ERROR
    }

    public class SQLQuery {
        // Struct
        public SQLQueryType type;
        public string error = null;


        // Query specific
        public string? CREATE_DATABASE_NAME;


        public string? CREATE_TABLE_NAME;
        public List<TableAttribute>? CREATE_TABLE_ATTRIBUTES;


        public string? CREATE_INDEX_NAME;
        public string? CREATE_INDEX_TABLE_NAME;
        public List<string>? CREATE_INDEX_TABLE_FIELDS;


        public string? DROP_DATABASE_NAME;
        public string? DROP_TABLE_NAME;


        public string? USE_DATABASE_NAME;


        public string? INSERT_TABLE_NAME;
        public List<KeyValuePair<string, string>>? INSERT_TABLE_ATTRIBUTES_VALUES;


        public string? DETELE_TABLE_NAME;
        public List<WhereCondition>? DELETE_TABLE_CONDITIONS;


        public SQLQuery(SQLQueryType type) {
            this.type = type;
        }
    }
}

