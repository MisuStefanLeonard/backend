using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.uAdressService;
using E_Commerce_BackEnd.Services.uService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace E_Commerce_BackEnd.Controllers;

[EnableCors("AllowVueApp")]
[Route("api/account")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAdressService _adressService;
    private readonly ILogger<Adrese> _adreseLogger;
    

    public UserController(IUserService userService, IAdressService adressService, ILogger<Adrese> adreseLogger)
    {
        _userService = userService;
        _adressService = adressService;
        _adreseLogger = adreseLogger;
    }
    
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        await Task.Delay(1);
        return Ok();
    }

    [HttpGet("profile/data")]
    [Authorize]
    public async Task<IActionResult> ProfilePersonalData()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var userClaims = HttpContext.User;
        // USER ID CLAIM
        var userIdClaim = userClaims.FindFirst("user_id");
        // USER ID
        int userId = int.Parse(userIdClaim!.Value);
            
        var data = await _userService.GetProfileDataAsync(userId);
        
        watch.Stop();
        Console.WriteLine($"In controller it took : {watch.ElapsedMilliseconds}");
        
        return Ok(data);
    }

    [HttpPost("profile/data")]
    [Authorize]
    public async Task<IActionResult> ChangePersonalData([FromBody] ConturiDto updatedDto)
    {
        var claims = HttpContext.User;
        var userIdFromClaim = claims.FindFirst(claim => claim.Type == "user_id");
        
        int userId = int.Parse(userIdFromClaim!.Value);
        
        int changingUserDataResponse = await _userService.EmailChangingOrUpdatingUserDataAsync(userId,updatedDto);

        // daca mail-ul a fost schimbat
        if (changingUserDataResponse == 1)
        {
            return NoContent(); // 204
        }
        // daca mail-ul nu a fost schimbat 
        else if (changingUserDataResponse == 0)
        {
            return Ok(); // 200
        }

        return BadRequest("A avut loc o eroare. Va rugam incercati mai tarziu");
    }
    

    [HttpPost("changeEmail/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPersonalDataChanging([FromRoute] string token , [FromBody] ConturiDto updatedDto)
    {
       
        if (token.Length != 100 || token.IsNullOrEmpty())
        {
            return BadRequest("Token is not present");
        }
       

        int response = await _userService.ModifyUserDataIfEmailHasChangedAsync(token, updatedDto);

        switch (response)
        {
            case 1:
            {
                return Ok("Data has been changed succesfully!");
            }
            case -1:
            {
                return NotFound("No account found with this token / Token expired !");
            }
            case 0:
            {
                return BadRequest("Error when trying to acces the link");
            }
        }

        return BadRequest("General error");
    }

    [HttpGet("profile/addresses")]
    [Authorize]
    public async Task<IActionResult> GetAdrese()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var claims = HttpContext.User;
        var userIdClaim = claims.FindFirst(claim => claim.Type == "user_id");
        if (userIdClaim == null)
        {
            return BadRequest("Token expired/Missing JWT Token/Not authorized/ Error");
        }
        
        var userId = int.Parse(userIdClaim.Value);

        var adrese = await _adressService.GetAllUsersAdresses(userId);

        if (adrese != null)
        {
            watch.Stop();
            _adreseLogger.LogInformation($"ADRESSES EMPTY : {watch.ElapsedMilliseconds}");
            return Ok(adrese);
        }
        _adreseLogger.LogInformation($"NO ADDRESS FETCHED DURATIONS : {watch.ElapsedMilliseconds}");
        return NotFound("No adresses found for this account");
    }
    

    [HttpPost("profile/addresses")]
    [Authorize]
    public async Task<IActionResult> SaveAddress([FromBody] AdreseDto adressDto)
    {
       
        
        var userId = int.Parse(HttpContext.User.FindFirst(claim => claim.Type == "user_id")!.Value);
        
        var response = await _adressService.SaveAddress(adressDto, userId);
        
        if (response == 1)
        {
            return Ok("Adress saved succesfully");
        }

        return BadRequest("Error when saving the address");
    }

    [HttpDelete("profile/addresses")]
    [Authorize]
    public async Task<IActionResult> DeleteAddress([FromBody] DeleteAddressDto deleteAddressDto)
    {
        var userId = int.Parse(HttpContext.User.FindFirst(claim => claim.Type == "user_id")!.Value);
        
        Console.WriteLine($" ALIASSSS => {deleteAddressDto.AddresToDeleteAliasDto}");

        var response = await _adressService.DeleteAddress(userId, deleteAddressDto.AddresToDeleteAliasDto!);

        if (response == 1)
        {
            return Ok("Address deleted succesfully");
        }

        return BadRequest("Error when deleting the address");

    }
    
}