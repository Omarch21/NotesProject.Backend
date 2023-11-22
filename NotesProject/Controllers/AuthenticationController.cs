using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson.IO;
using NotesProject.User;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
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
                    Message = "Registered successfully"
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
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("gliuosybo7433v1564dstjhljkhasdf78asflasd324fadaszzsdf567ngyfkjhnre957"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
                var expires = DateTime.Now.AddDays(1);

                var token = new JwtSecurityToken(
                   // issuer: "https://localhost:4200",
                 //  audience: "https://localhost:4200",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                    );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                HttpContext.Response.Cookies.Append("X-Access-Token", tokenString,
                new CookieOptions
                {
                    Expires = DateTime.Now.AddMinutes(10),
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.None
                });
                var refreshToken1 = GenerateRefreshToken();
                HttpContext.Response.Cookies.Append("X-Refresh-Token", refreshToken1.Token,
                 new CookieOptions
                 {
                     Expires = refreshToken1.Expires,
                     HttpOnly = true,
                     Secure = true,
                     IsEssential = true,
                     SameSite = SameSiteMode.None
                 });
                user.Token = refreshToken1.Token;
                user.TokenCreated = refreshToken1.Created;
                user.TokenExpires = refreshToken1.Expires;
                await _userManager.UpdateAsync(user);
                
                return new LoginResponse
                {
                    AccessToken = refreshToken1.Token,
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
       
        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddDays(1),
                Created = DateTime.Now
            };
            return refreshToken;
        }

        [HttpGet("RefreshToken")]
        public async Task<ActionResult<string>>  RefreshToken()
        {
           
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            var user = _userManager.Users.Where(x => x.Token == refreshToken).FirstOrDefault();
            if(user == null || user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token is expired");
            }

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
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("gliuosybo7433v1564dstjhljkhasdf78asflasd324fadaszzsdf567ngyfkjhnre957"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
                var expires = DateTime.Now.AddDays(1);

                var token = new JwtSecurityToken(
                   // issuer: "https://localhost:4200",
                 //  audience: "https://localhost:4200",
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                    );
                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            HttpContext.Response.Cookies.Append("X-Access-Token", tokenString,
                 new CookieOptions
                 {
                     Expires = DateTime.Now.AddMinutes(10),
                     HttpOnly = true,
                     Secure = true,
                     IsEssential = true,
                     SameSite = SameSiteMode.None
                 });
            var refreshToken1 = GenerateRefreshToken();
            HttpContext.Response.Cookies.Append("X-Refresh-Token", refreshToken1.Token,
                new CookieOptions
                {
                    Expires = refreshToken1.Expires,
                    HttpOnly = true,
                    Secure = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.None
                });
            _userManager.Users.Where(x => x.UserName == user.UserName).First().Token = refreshToken1.Token;
            _userManager.Users.Where(x => x.UserName == user.UserName).First().TokenCreated = refreshToken1.Created;
            _userManager.Users.Where(x => x.UserName == user.UserName).First().TokenExpires = refreshToken1.Expires;

            return Ok();
                }

        //public void SetRefreshToken(RefreshToken refreshToken, ApplicationUser user)
        //{
        //    HttpContext.Response.Cookies.Append("X-Refresh-Token", refreshToken.Token,
        //        new CookieOptions
        //        {
        //            Expires = refreshToken.Expires,
        //            HttpOnly = true,
        //            Secure = true,
        //            IsEssential = true,
        //            SameSite = SameSiteMode.None
        //        });
        //    _userManager.Users.Where(x => x.UserName == user.UserName).First().Token = refreshToken.Token;
        //    _userManager.Users.Where(x => x.UserName == user.UserName).First().TokenCreated = refreshToken.Created;
        //    _userManager.Users.Where(x => x.UserName == user.UserName).First().TokenExpires = refreshToken.Expires;

        //}

        //public void SetJWT(string encrypterToken)
        //{
        //    HttpContext.Response.Cookies.Append("X-Access-Token",encrypterToken,
        //        new CookieOptions
        //        {
        //            Expires = DateTime.Now.AddMinutes(10),
        //            HttpOnly = true,
        //            Secure = true,
        //            IsEssential = true,
        //            SameSite = SameSiteMode.None
        //        }); 
        //}

        [HttpDelete("RevokeToken/{username}")]
        public async Task<IActionResult> RevokeToken(string username)
        {
           var user =  await _userManager.FindByEmailAsync(username);
            user.Token = "";
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpDelete("logout")]
        public async Task<IActionResult> Logout()
        {

            Response.Cookies.Delete("X-Access-Token", new CookieOptions
            {
                Expires = DateTime.Now.AddMinutes(-1),
                   HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = true,
                IsEssential = true

            }); ;
                return Ok(new { message = "Logout successful" });
         
           
         
        }
        
    }
}
