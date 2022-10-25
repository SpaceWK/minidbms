using System;
using System.Collections.Generic;

namespace Server {
    public enum SQLQueryType {
        CREATE_DATABASE,
        CREATE_TABLE,
        CREATE_INDEX,

        DROP_DATABASE,
        DROP_TABLE
    }

    public class SQLQuery {
        public SQLQueryType type;


        public string? CREATE_DATABASE_NAME;


        public string? CREATE_TABLE_NAME;
        public Dictionary<string, string>? CREATE_TABLE_ATTRIBUTES;


        public string? CREATE_INDEX_NAME;
        public string? CREATE_INDEX_TABLE_NAME;
        public string? CREATE_INDEX_STRUCTURE;


        public string? DROP_DATABASE_NAME;
        public string? DROP_TABLE_NAME;


        public SQLQuery(SQLQueryType type) {
            this.type = type;
        }
    }
}

