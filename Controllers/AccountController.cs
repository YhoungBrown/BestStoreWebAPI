using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly EmailSender emailSender;

        public AccountController(IConfiguration configuration, ApplicationDbContext context, EmailSender emailSender)
        {
            // Constructor logic if needed
            this.configuration = configuration;
            this.context = context;
            this.emailSender = emailSender;
        }


        [HttpPost("Register")]
        public IActionResult Register(UserDto userDto)
        {
           
            //check if user Email already exist in the database
            var existingEmailCount = context.Users.Count(u => u.Email == userDto.Email);

            if (existingEmailCount > 0)
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return BadRequest(ModelState);
            }

            // Create a new User object from the UserDto
                //encrypt the password

            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            //creating the account 
            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Address = userDto.Address,
                Password = encryptedPassword,
                Role = "client" 
            };

            // Add the user to the database
            context.Users.Add(user);
            context.SaveChanges();

            // Create JWT token for the user
            string jwt = createJwtToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            var response = new
            {
                JwtToken = jwt,
                UserProfile = userProfileDto
            };



            return Ok(response);
        }


        [HttpPost("Login")]
        public IActionResult Login(string email, string password)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            { 
                ModelState.AddModelError("Email", "User not found , pls recheck the email.");
                return BadRequest(ModelState);
            }

            //verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Incorrect password.");
                return BadRequest(ModelState);
            }

            // Create JWT token for the user
            string jwt = createJwtToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            var response = new
            {
                JwtToken = jwt,
                UserProfile = userProfileDto
            };
            return Ok(response);
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email)
        {
            var user = context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                return NotFound();
            }


            //delet any old password request for the same email
            var oldPasswordReset = context.PasswordResets.FirstOrDefault(pr => pr.Email == email);
            if (oldPasswordReset != null)
            {
                context.Remove(oldPasswordReset);
            }

            // Create a new password reset request
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            var pwdReset = new PasswordReset()
            {
                Email = email,
                Token = token,
                ExpirationDate = DateTime.Now.AddHours(1) // Set expiration time to 1 hour
            };

            context.PasswordResets.Add(pwdReset);
            context.SaveChanges();

            // Send email with the reset token (you can implement this in EmailSender service)
            {

                string EmailSubject = "Password Reset Request";
                string toEmail = user.Email;
                string MailRecievingUser = user.FirstName + " " + user.LastName;
                string EmailMessage = $"Hello {MailRecievingUser},\n\n" +
                                      "You have requested a password reset. " +
                                      "Please click the link below to reset your password:\n" +
                                      $"https://yourapp.com/reset-password?token={token}\n\n" + $"if the link doesnt work, copy this token and paste it in the app: {token}\n\n" +
                                      "This link will expire in 1 hour.\n\n" +
                                      "Best regards,\nYour App Team";

                emailSender.SendEmail(EmailSubject, toEmail, MailRecievingUser, EmailMessage).Wait();

                return Ok();
            }
        }


        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(string token, string newPassword)
        { 
            var pwdReset = context.PasswordResets.FirstOrDefault(u => u.Token == token);

            if (pwdReset == null || pwdReset.ExpirationDate < DateTime.Now)
            {
                ModelState.AddModelError("Token", "Invalid or expired token.");
                return BadRequest(ModelState);
            }
            // Find the user by email

            var user = context.Users.FirstOrDefault(u => u.Email == pwdReset.Email);
            if (user == null)
            {
                ModelState.AddModelError("Email", "User not found.");
                return BadRequest(ModelState);
            }

            var passwordHasher = new PasswordHasher<User>();
           string encryptedPassword = passwordHasher.HashPassword(new User(), newPassword);

            // Update the user's password

            user.Password = encryptedPassword;

            //delete the token since its been used to prevent reuse
            context.PasswordResets.Remove(pwdReset);

            context.SaveChanges();
            return Ok("Password Reset Successfully");
        }


        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            int id = JWTReader.GetUserId(User);

            var user = context.Users.Find(id);
            if (user == null)
            {
                return Unauthorized();
            }


            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return Ok(userProfileDto);
        }


        [Authorize]
        [HttpPut("updateProfile")]
        public IActionResult UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = JWTReader.GetUserId(User);
            var user = context.Users.Find(id);

            if (user == null)
                return Unauthorized();

            // Update user properties
            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone ?? "";
            user.Address = userProfileUpdateDto.Address;

            context.SaveChanges();

            var updatedUserProfile = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };


            return Ok(updatedUserProfile);
        }


        [Authorize]
        [HttpPut("updatePassword")]
        public IActionResult UpdatePassword([Required, MinLength(8), MaxLength(100)] string newPassword)
        {
            int id = JWTReader.GetUserId(User);
            var user = context.Users.Find(id);
            if (user == null)
                return Unauthorized();

            // Encrypt the new password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(user, newPassword);

            // Update the user's password
            user.Password = encryptedPassword;
            context.SaveChanges();

            return Ok("Password Updated Successfully");
        }

            [Authorize]
        [HttpGet("AuthorizeAuthenticatedUsers")]
        public IActionResult AuthorizeAuthenticatedUsers()
        {
            // This endpoint is protected and can only be accessed by authenticated users
            return Ok("You are authenticated!");
        }


        [Authorize(Roles = "admin")]
        [HttpGet("AuthorizeAdmin")]
        public IActionResult AuthorizedAdmin()
        {
            // This endpoint is protected and can only be accessed by authenticated users
            return Ok("You are authenticated Admin!");
        }


        [Authorize(Roles = "admin,seller")]
        [HttpGet("AuthorizedAdminAndSeller")]
        public IActionResult AuthorizedAdminAndSeller()
        {
            // This endpoint is protected and can only be accessed by authenticated users
            return Ok("You are authenticated Admin or Seller!");
        }


        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        { 
            var identity = User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Ok("No Available Claims");
            }

            Dictionary<string, string> claims = new Dictionary<string, string>();
            foreach (var claim in identity.Claims)
            {
                claims.Add(claim.Type, claim.Value);
            }

            return Ok(claims);
        }




        /*  [HttpGet("TestToken")]
          public IActionResult TestToken()
          {
              User user = new User()
              {
                  Id = 2,
                  Role = "admin"
              };

              string jwt = createJwtToken(user);

              var response = new { JwtToken = jwt };

              return Ok(response);
          }*/


        private string createJwtToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim("id", "" + user.Id),
                new Claim("role", user.Role),
            };


            string secretKey = configuration["JwtSettings:key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                    issuer : configuration["JwtSettings:Issuer"],
                    audience: configuration["JwtSettings:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}
