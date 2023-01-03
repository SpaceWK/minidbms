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

        SELECT,

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


        public string? DELETE_TABLE_NAME;
        public List<WhereCondition>? DELETE_TABLE_CONDITIONS;


        // SELECT a, b, c FROM table WHERE a = 1;
        public bool? SELECT_DISTINCT = false; // SELECT DISTINCT Country FROM Customers;
        public List<string>? SELECT_PROJECTION; // a, b, c
        public string? SELECT_TABLE_NAME; // table
        public List<WhereCondition>? SELECT_SELECTION; // WHERE a = 1

        // SELECT table.a, table.b, table.c FROM table INNER JOIN table ON tableX.id = tableY.id;
        public List<KeyValuePair<string,string>>? SELECT_JOIN_PROJECTION;
        public string? SELECT_JOIN_FIRST_TABLE;
        public string? SELECT_JOIN_SECOND_TABLE;
        public KeyValuePair<string, string>? SELECT_JOIN_FIRST_SELECTION;
        public KeyValuePair<string, string>? SELECT_JOIN_SECOND_SELECTION;

        public SQLQuery(SQLQueryType type) {
            this.type = type;
        }
    }
}

