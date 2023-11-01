using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace NotesProject
{
    public class Notecard
    {
        [BsonId]
        public ObjectId id { get; set; }

        [BsonElement("position")]
        public int Position { get; set; }

        [BsonElement("question")]
        public string Question { get; set; }

        [BsonElement("answer")]
        public string Answer { get; set; }

        [BsonElement("subject")]
        public string Subject { get; set; }
    }
}
