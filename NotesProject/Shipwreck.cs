using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotesProject
{
    [BsonIgnoreExtraElements]
    public class Shipwreck
    {
        [BsonId]
        public ObjectId id { get; set; }

        [BsonElement("feature_type")]
        public string FeatureType { get; set; }
        [BsonElement("chart")]
        public string Chart { get; set; }
        [BsonElement("latdec")]
        public double Latitute { get; set; }
        [BsonElement("longdec")]
        public double Lonitute { get; set;}
    }
}
