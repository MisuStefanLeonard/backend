
using E_Commerce_BackEnd.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.uService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.IdentityModel.Tokens;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace E_Commerce_BackEnd.Controllers
{   
    [EnableCors("AllowVueApp")]
    [Route("api/user")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthenticationController(IUserService service)
        {
            _userService = service;
        }

        // GET: api/user/getUsers
        [Authorize]
        [HttpGet("getUsers")]
        public async Task<ActionResult<IEnumerable<Conturi>>> GetAccountsAsync()
        {
            var accounts = await _userService.GetAllAccountsAsync();
            if (accounts == null || !accounts.Any())
            {
                return NotFound();
            }
            return Ok(accounts);
        }
        
        [HttpGet("getUserByUsername/{username}")]
        [AllowAnonymous]
        public async Task<ActionResult<Conturi>> GetAccountByUsernameAsync(string username)
        {
            var account = await _userService.GetAccountByEmailAsync(username);

            if (account == null)
            {
                return NotFound();
            }

            return Ok();
        }
        
        [HttpGet("getUserByEmail/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<Conturi>> GetAccountByEmailAsync(string email)
        {
            var account = await _userService.GetAccountByEmailAsync(email);

            if (account == null)
            {
                return NotFound();
            }

            return Ok(account.Email);
        }
        
        // GET: api/user/get/5
        [HttpGet("getUser/{id}")]
        [Authorize]
        public async Task<ActionResult<Conturi>> GetAccountAsync(int id)
        {
            var account = await _userService.GetAccountByIdAsync(id);

            if (account == null)
            {
                return NotFound();
            }

            return account;
        }

        // PUT: api/user/update/5
        [Authorize]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateAccount(int id, Conturi updatedAccount)
        {
            if (id != updatedAccount.IdCont)
            {
                return BadRequest();
            }
            
            try
            {
                await _userService.UpdateAccountAsync(id, updatedAccount);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await AccountExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/user/addUser
        [HttpPost("inregistrare")]
        [AllowAnonymous]
        public async Task<ActionResult<Conturi>> AddAccount(Conturi conturi)
        {
            await _userService.AddAccountAsync(conturi);
            return Ok();
        }
        
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<Conturi>> LoginAccount([FromBody]LoginDto loginDto)
        {
            try
            {
                var user = await _userService.LoginAccountAsync(loginDto);
                var isAdmin = user is { RoleProp: "Admin" };
                if (user == null)
                {
                    return Unauthorized(new {message = "Invalid credentials or account is not verified"});
                }
               
                var cookieOptions = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(1)
                };
                var isLoggedInCookieOptions = new CookieOptions()
                {
                    Expires = DateTime.UtcNow.AddDays(1)
                };
                Response.Cookies.Append("JWTToken", loginDto.TokenProp, cookieOptions);
                Response.Cookies.Append("userLoggedIn" , "1" , isLoggedInCookieOptions);
                Response.Cookies.Append("admin" , isAdmin ? "1" : "0" , isLoggedInCookieOptions);
                return Ok(new { message = "Succesfull login" });
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return StatusCode(500, new { message = "An error occurred while processing your request." });
            }
        }

        [HttpGet("signin-google")]
        [AllowAnonymous]
        public IActionResult GoogleLogIn()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Authentication");
           
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
           
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;

            var newCreatedGoogleUser = _userService.CreateAccountBasedOnGoogleLogIn(claims!);

            var newJwtTokenForGoogleUser = _userService.GenerateJwt(newCreatedGoogleUser.Result!);
            
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(2)
            };
            var isLoggedInCookieOptions = new CookieOptions()
            {
                Expires = DateTime.UtcNow.AddHours(2)
            };
            Response.Cookies.Append("JWTToken", newJwtTokenForGoogleUser, cookieOptions);
            Response.Cookies.Append("userLoggedIn" , "1" , isLoggedInCookieOptions);

            return Redirect("http://localhost:8080/home");
        }

        [HttpPost("checkPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckPassword([FromBody]CheckPasswordDto checkPasswordDto)
        {
            try
            {
                
                var response = await _userService
                    .CheckPasswordInDbAsync(checkPasswordDto.TokenProp , checkPasswordDto.HashedPasswordDtoProp);
                
                if (!response)
                {
                    return Ok("Password is not the same as the old one");
                }

                return NotFound("Password is the same the old one");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpPost("forgotpassword")]
        [AllowAnonymous]
        public async Task<IActionResult> PostForgotPassword([FromBody] ForgotPasswordEmailDto forgotPasswordEmailDto)
        {
            try
            {
                
                var response = await _userService.ForgotPasswordAsync(forgotPasswordEmailDto.EmailProp);
               
                if (response == 1)
                {
                    return Ok();
                }

                return NotFound("Account not found");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet("forgotpassword/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPasswordUpdate([FromRoute] string token)
        {
            try
            {
                Console.WriteLine("IN FORGOTPASSWORD TOKEN");
                if (token.Length != 100 || token.IsNullOrEmpty())
                {
                    return NotFound("Token is not present"); // 400
                }

                var response = await _userService.CheckForgotPasswordTokenLifeTime(token);
                Console.WriteLine(" in backend dupa response");
                if (response)
                {
                    return Ok("Token not yet expired");
                }

                return NotFound("Token expired"); // 400


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [HttpPost("forgotpassword/{token}")]
        [AllowAnonymous]

        public async Task<IActionResult> PostForgotPasswordUpdate([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                
                var response = await _userService.ChangePasswordAsync(changePasswordDto);

                switch (response)
                {
                    case 1:
                        return Ok("Successfully changed the password"); // 200
                    case -1:
                        return BadRequest("User does not exist in the database"); // 400
                    case 0:
                        return NotFound("Token already expired"); // 404
                    case -2:
                        return StatusCode(600, "Concurrency update in the database! Rolling back the transaction");
                    default:
                        return StatusCode(500, "Unknown error occurred"); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var googleGeneratedCookie = Request.Cookies[".AspNetCore.Cookies"];
                
                var cookieOptions = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(-2)
                };

                var isLoggedInCookieOptions = new CookieOptions()
                {
                    Expires = DateTime.UtcNow.AddDays(-2)
                };

               

                Response.Cookies.Append("JWTToken", "", cookieOptions);
                Response.Cookies.Append("userLoggedIn" , "" , isLoggedInCookieOptions);
                Response.Cookies.Append("admin" , "" , isLoggedInCookieOptions);
                Response.Cookies.Append("adminLoggedIn" , "" , isLoggedInCookieOptions);

                if (googleGeneratedCookie != null)
                {
                    Response.Cookies.Append(".AspNetCore.Cookies", "", cookieOptions);
                }
              
                await Task.Delay(10);

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logout failed:" + ex.Message);
                return Unauthorized();
            }
        }

        // DELETE: api/user/deleteUser/5
        [HttpDelete("deleteUser/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteConturi(int id)
        {
            await _userService.RemoveAccountAsync(id);
            
            return NoContent();
        }

        private async Task<bool> AccountExists(int id)
        {
            var account = await _userService.GetAccountByIdAsync(id);
            return account != null;
        }
        

        [HttpGet("confirmare/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> Confirmare([FromRoute] string token)
        {
           
            if (token.Length != 100 || token.IsNullOrEmpty())
            {
                return BadRequest("Token is not present");
            }
            
            Console.WriteLine("In controller confirmare");
            
            int response = await _userService.AccountConfirmationAsync(token);
         
            Console.WriteLine("In controller confirmar dupa responsee " + response);
            if (response == 1)
            {
                return Ok();
            }

            if (response == 3)
            {
                return NotFound("Token expired! Resend the activation link");
            }

            
            return BadRequest("Something happened in the confirmation function / Go see UserService ");
        }
        
        [HttpPost("confirmare/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmare([FromRoute] string token)
        {
            if (token.Length != 100 || token.IsNullOrEmpty())
            {
                return BadRequest("Token is not present");
            }

            int response = await _userService.ResendConfirmationMailAsync(token);

            if (response == 1)
            {
                return Ok();
            }

            if (response == -1)
            {
                return NotFound("Account not found!");
            }

            return BadRequest("Something happened in the ResendConfirmare function / Go see UserService ");
        }
    }
}
