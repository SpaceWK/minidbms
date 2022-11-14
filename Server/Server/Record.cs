using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    public class Record {
        [BsonId]
        public string key { get; set; }
        public string value { get; set; }

    public Record(string key, string value) {
            this.key = key;
            this.value = value;
        }
    }
}
