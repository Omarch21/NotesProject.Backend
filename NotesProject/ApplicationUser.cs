using Microsoft.AspNetCore.Identity.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace NotesProject
{
    public class User : IdentityUser
    {
        [BsonId]
        public ObjectId id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }
        [BsonElement("email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        [Required]
        public string Email { get; set; }
        [BsonElement("password")]
        [Required]
        public string Password { get; set; }

    }
}
