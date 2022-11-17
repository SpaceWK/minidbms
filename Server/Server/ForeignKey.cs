using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    public class ForeignKey {
        public string attribute;
        public string referencedTableName;
        public string referencedTableKey;

        public ForeignKey(string attribute, string referencedTableName, string referencedTableKey) {
            this.attribute = attribute;
            this.referencedTableName = referencedTableName;
            this.referencedTableKey = referencedTableKey;
        }
    }
}
