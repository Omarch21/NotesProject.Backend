using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;

namespace NotesProject.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private IMongoCollection<NotecardList> _noteCollection;
        public ValuesController(IMongoClient client)
        {
            var database = client.GetDatabase("Notecards");
            _noteCollection = database.GetCollection<NotecardList>("Notecard_list");
        }
        [HttpGet]
        public IEnumerable<NotecardList> GetTest()
        {
            return _noteCollection.Find(s => s.Name == "Test").ToList();
        }
        [HttpPost]
        public IActionResult CreateSet([FromBody] NotecardList new_notecards)
        {
            try
            {
                Console.WriteLine(GetUser());
                new_notecards.UserId = GetUser();
                _noteCollection.InsertOne(new_notecards);
                return Ok("Notecard inserted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
        private string GetUser()
        {
            ClaimsPrincipal user = HttpContext.User;
            Claim userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if(userIdClaim != null)
            {
                return userIdClaim.Value;
            }
            return null;
        }
        [HttpPost("Card")]
        public IActionResult InsertNotecard([FromBody] Notecard new_notecard, string set_id)
        {
            try
            {
                new_notecard.id = ObjectId.GenerateNewId();

                var filter = Builders<NotecardList>.Filter.Eq("_id", ObjectId.Parse(set_id));

                var fetchLastCard = Builders<NotecardList>.Projection.Slice("notecards", -1);
                var lastcard = _noteCollection.Find(filter).Project<NotecardList>(fetchLastCard).FirstOrDefault();
                if(lastcard != null && lastcard.Notecard_list.Count > 0)
                {
                    var new_position = lastcard.Notecard_list[lastcard.Notecard_list.Count-1];
                    new_notecard.Position = new_position.Position + 1;
                }
                var pushCard = Builders<NotecardList>.Update.Push("notecards", new_notecard);
                _noteCollection.UpdateOne(filter,pushCard);
                return Ok("Notecard inserted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}
