using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    public enum ComparisonOperator {
        EQUAL,
        NOT_EQUAL,
        LESS_THAN,
        LESS_OR_EQUAL_THAN,
        GREATER_THAN,
        GREATER_OR_EQUAL_THAN
    }

    public class WhereCondition {
        public string name;
        public ComparisonOperator comparison;
        public string value;

        public WhereCondition(string name, ComparisonOperator comparison, string value) {
            this.name = name;
            this.comparison = comparison;
            this.value = value;
        }
    }
}
