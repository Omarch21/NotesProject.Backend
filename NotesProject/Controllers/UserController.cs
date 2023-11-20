using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace NotesProject.Controllers
{
    [Authorize]
    [ApiController]

    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("GetUser")]
        public Task<ApplicationUser> GetUser(string email)
        {
            return _userManager.FindByEmailAsync(email);

        }
        [HttpGet("test")]
        public IActionResult GetTest()
        {
            return Ok("test");
        }
        
    }
}
