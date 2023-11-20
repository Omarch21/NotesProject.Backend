using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NotesProject.User;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace NotesProject.Controllers
{
    [ApiController]
    [Route("api/authentication")]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AuthenticationController(UserManager<ApplicationUser> userManager,RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [HttpPost]
        [Route("roles/add")]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var appRole = new ApplicationRole { Name = request.Role };
            var createRole = await _roleManager.CreateAsync(appRole);

            return Ok(new { Message = "Role created" });
        }
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await RegisterAsync(request);

            return result.Success ? Ok(result) : BadRequest(result.Message);

        }

      

        private async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var userExists = await _userManager.FindByEmailAsync(request.Email);
                if(userExists != null) return new RegisterResponse { Message = "User already exists", Success = false };//checks if user already exists

                userExists = new ApplicationUser
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    UserName = request.Email
                };

                var createUserResult = await _userManager.CreateAsync(userExists, request.Password);
                if(!createUserResult.Succeeded) return new RegisterResponse { Message = $"Creating user failed {createUserResult?.Errors.First().Description}", Success = false };//gives more description of the error




                var addUserToRoleResult = await _userManager.AddToRoleAsync(userExists, "USER");//adding role to the user
                if(!addUserToRoleResult.Succeeded) return new RegisterResponse { Message = $"Created successfully, but could not assign role, {addUserToRoleResult?.Errors?.First()?.Description}", Success = false };

                return new RegisterResponse
                {
                    Success = true,
                    Message = "User registered successfully"
                };
            }
            catch(Exception ex)
            {
                return new RegisterResponse { Message = ex.Message, Success = false };
            }
        }

        [HttpGet("CheckUserLoggedIn")]
        public async Task<IActionResult> CheckUserLoggedIn()
        {
            var result = await CheckUserLoggedInMessage();
            return result.Success ? Ok(result) : BadRequest(result);
        }
            private async Task<LoggedInResponse> CheckUserLoggedInMessage()
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;

           if(userid != null && username != null)
            {
                return new LoggedInResponse
                {
                    Success = true,
                    Message = "Logged in",
                    LoggedInUserID = userid,
                    LoggedInUsername = username,
                };
            }
           else
            {
                return new LoggedInResponse
                {
                    Success = false,
                    Message = "Not Logged in",
                };
            }
        }


        [HttpPost]
        [Route("login")]
        [ProducesResponseType((int) HttpStatusCode.OK,Type = typeof(LoginResponse))]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await LoginAsync(request);
            return result.Success ? Ok(result) : BadRequest(result.Message);
        }
        private async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user is null) return new LoginResponse { Message = "Invalid email/password", Success = false }; //checks if user exists, if not return response saying invalid email and it was not successful

                var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString())
            };
                var roles = await _userManager.GetRolesAsync(user);
                var roleClaims = roles.Select(x => new Claim(ClaimTypes.Role, x));
                claims.AddRange(roleClaims);
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("gliuosybo7433v1564dstjhljkhasdf78asfl"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.AddDays(1);

                var token = new JwtSecurityToken(
                   // issuer: "https://localhost:4200",
                   // audience: "https://localhost:4200",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                    );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                Response.Cookies.Append("token", tokenString, new CookieOptions
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddDays(1),
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                }); ;

                return new LoginResponse
                {
                    AccessToken = tokenString,
                    Message = "Login Successful",
                    Email = user?.Email,
                    Success = true,
                    UserId = user?.Id.ToString()
                };
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new LoginResponse { Success = false, Message = ex.Message };
            }
        }
        [HttpDelete("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                Response.Cookies.Delete("token");
                return Ok(new { message = "Logout successful" });
            }catch(Exception ex)
            {
                return StatusCode(500, new { message = "an error occured" });
            }
           
         
        }
        
    }
}
