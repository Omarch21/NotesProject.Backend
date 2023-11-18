using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace NotesProject

{
    [BsonIgnoreExtraElements]
    public class NotecardList
    {
        [BsonId]
        public ObjectId id { get; set; }

        [BsonElement("name")]

        public string Name { get; set; }

        [BsonElement("notecards")]
        public List<Notecard> Notecard_list { get; set; } 
        public string UserId { get; set; }

    }
}
