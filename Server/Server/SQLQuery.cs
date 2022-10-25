using System;
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
        public string[] args;

        public SQLQuery(SQLQueryType type, string[] args) {
            this.type = type;
            this.args = args;
        }
    }
}

