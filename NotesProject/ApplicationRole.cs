using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System.Runtime.Serialization;

namespace NotesProject
   
{
    [CollectionName("roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
    }
}
