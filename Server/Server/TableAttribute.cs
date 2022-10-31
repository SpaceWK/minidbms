using System;

namespace Server {
    public enum TableAttributeType {
        INT,
        VARCHAR
    }
    public class TableAttribute {
        string name;
        TableAttributeType type;
        int length;

        bool isNull = false;
        bool isUnique = false;

        bool isPrimaryKey = false;
        bool isForeignKey = false;
        string foreignKeyTableReferenceName = null;
        string foreignKeyTableReferenceKey = null;

        public TableAttribute(
            string name, 
            TableAttributeType type, 
            int length, 

            bool isNull = false, 
            bool isUnique = false,

            bool isPrimaryKey = false,
            bool isForeignKey = false,
            string foreignKeyTableReferenceName = null,
            string foreignKeyTableReferenceKey = null
        ) {
            this.name = name;
            this.type = type;
            this.length = length;

            this.isNull = isNull;
            this.isUnique = isUnique;

            this.isPrimaryKey = isPrimaryKey;
            this.isForeignKey = isForeignKey;
            this.foreignKeyTableReferenceName = foreignKeyTableReferenceName;
            this.foreignKeyTableReferenceKey = foreignKeyTableReferenceKey;
        }
    }
}
