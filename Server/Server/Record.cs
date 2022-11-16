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

        public string getKeyValue(string key, List<string> structure) {
            int index = 0;
            string[] values = this.value.Split("#");
            foreach (string item in structure) {
                if (item == key) {
                    return values[index];
                }

                index++;
            }

            return null;
        }
    }
}
