using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FoodAppIdentity.Helpers;
using FoodAppIdentity.Infrastructure;
using FoodAppIdentity.Models;
using FoodAppIdentity.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace FoodAppIdentity.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private IdentityDbContext db;
        private IConfiguration config;

        public IdentityController(IdentityDbContext dbContext, IConfiguration configuration)
        {
            db = dbContext;
            config = configuration;
        }

        [AllowAnonymous]
        [HttpPost("register", Name = "RegisterUser")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<dynamic>> Register(User user)
        {
            TryValidateModel(user);
            if (ModelState.IsValid)
            {

                user.UserDetailModify();
                var usr = db.Users.Any(u => u.Username == user.Username);
                if (usr == true)
                {

                    var conflict = new
                    {
                        Description = "Username already exists"
                    };
                    return Conflict(conflict);
                }
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
                if (user.Role == AppConstants.Admin || user.Role == AppConstants.HotelAdmin)
                {
                    await SendVerificationMailAsync(user);
                }
                return Created("", new
                {
                    user.Id,
                    user.Fullname,
                    user.Username,
                    user.Email
                });
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
        [Authorize]
        [HttpGet("", Name = "GetUsers")]
        public ActionResult<List<UserV>> GetUsers()
        {
            var result = this.db.Users.ToList();
            List<UserV> userVs = new List<UserV>();
            foreach (User u in result)
            {
                UserV userV = new UserV
                {
                    Id = u.Id,
                    Fullname = u.Fullname,
                    Email = u.Email,
                    Role = u.Role
                };
                userVs.Add(userV);
            }
            return userVs;
        }
        [Authorize]
        [HttpPost("adduser", Name = "AddUser")]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<dynamic>> AddUser(User user)
        {
            TryValidateModel(user);
            if (ModelState.IsValid)
            {

                user.UserDetailModify();
                var usr = db.Users.Any(u => u.Username == user.Username);
                if (usr == true)
                {

                    var conflict = new
                    {
                        Description = "Username already exists"
                    };
                    return Conflict(conflict);
                }
                await db.Users.AddAsync(user);
                await db.SaveChangesAsync();
                return Created("", new
                {
                    user.Id,
                    user.Fullname,
                    user.Username,
                    user.Email
                });
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
        [AllowAnonymous]
        [HttpPost("token", Name = "GetToken")]
        public ActionResult<dynamic> GetToken(LoginModel model)
        {
            TryValidateModel(model);
            if (ModelState.IsValid)
            {
                var user = db.Users.SingleOrDefault(s => s.Username == model.Username && s.Password == model.Password && (s.Status == AppConstants.Verified || s.Status == AppConstants.NotApplicable) && s.IsDeleted == false);
                if (user != null)
                {
                    var token = GenerateToken(user);
                    return Ok(new { user.Fullname, user.Email, user.Username, user.Role, Token = token });
                }
                else
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [Authorize]
        [HttpPost("duser", Name = "DeleteUser")]
        public ActionResult<dynamic> DeleteUser([FromBody] DeleteUser user)
        {
            if (string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.selfusername))
            {
                return BadRequest();
            }
            else
            {
                var admin = db.Users.FirstOrDefault(u => u.Username == user.selfusername && u.Status == AppConstants.Verified);
                if (admin?.Role == AppConstants.Admin)
                {
                    var usr = db.Users.FirstOrDefault(u => u.Username == user.username);
                    if (usr == null)
                    {
                        return NotFound();
                    }
                    usr.IsDeleted = true;
                    var result = db.Users.Update(usr);
                    db.SaveChanges();
                    return NoContent();

                }
                else
                {
                    return Unauthorized();
                }
            }

        }
        [NonAction]
        private string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Fullname),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, "identityapi"));

            claims.Add(new Claim(ClaimTypes.Role, user.Role));

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.GetValue<string>("Jwt:secret")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: config.GetValue<string>("Jwt:issuer"),
                audience: null,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
            );
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenString;
        }

        [NonAction]
        private async Task SendVerificationMailAsync(User user)
        {
            var userObj = new
            {
                user.Id,
                user.Fullname,
                user.Email,
                user.Username
            };
            var messageText = JsonConvert.SerializeObject(userObj);
            StorageAccountHelper helper = new StorageAccountHelper();
            helper.StorageConnectionString = config.GetConnectionString("StorageConnection");
            await helper.SendMessageAsync(messageText, "users");
        }
    }
}
