using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.ClientOrdersDto;
using E_Commerce_BackEnd.Models.UserRelatedModels;
using E_Commerce_BackEnd.Services.uAdminService;
using E_Commerce_BackEnd.Services.uAdressService;
using E_Commerce_BackEnd.Services.uGeneralService;
using E_Commerce_BackEnd.Services.uOrdersService;
using E_Commerce_BackEnd.Services.uPopUpService;
using E_Commerce_BackEnd.Services.uService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Controllers;

[EnableCors("AllowVueApp")]
[Route("api/account")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAdressService _adressService;
    private readonly ILogger<Adrese> _adreseLogger;
    private readonly IAdminService _adminService;
    private readonly IPopUpService _popUpService;
    private readonly IMemoryCache _cache;
    private readonly IGeneralSettingsService _generalSettingsService;
    private readonly IOrderService _orderService;
    

    public UserController(IUserService userService, IAdressService adressService, 
        ILogger<Adrese> adreseLogger, IAdminService adminService, IMemoryCache cache, IGeneralSettingsService generalSettingsService, IOrderService orderService, IPopUpService popUpService)
    {
        _userService = userService;
        _adressService = adressService;
        _adreseLogger = adreseLogger;
        _adminService = adminService;
        _cache = cache;
        _generalSettingsService = generalSettingsService;
        _orderService = orderService;
        _popUpService = popUpService;
    }
    
    [HttpGet("profile")]
    [Authorize]
    public IActionResult Profile()
    {
        return Ok();
    }

    [HttpGet("profile/data")]
    [Authorize]
    public async Task<IActionResult> ProfilePersonalData()
    {
        
        var userClaims = HttpContext.User;
        // USER ID CLAIM
        var userIdClaim = userClaims.FindFirst("user_id");
        // USER ID
        var userId = int.Parse(userIdClaim!.Value);
            
        var data = await _userService.GetProfileDataAsync(userId);
        
        return Ok(data);
    }

    [HttpPost("profile/data")]
    [Authorize]
    public async Task<IActionResult> ChangePersonalData([FromBody] ConturiDto updatedDto)
    {
        var claims = HttpContext.User;
        var userIdFromClaim = claims.FindFirst(claim => claim.Type == "user_id");
        
        var userId = int.Parse(userIdFromClaim!.Value);
        
        var changingUserDataResponse = await _userService.EmailChangingOrUpdatingUserDataAsync(userId,updatedDto);

        return changingUserDataResponse switch
        {
            // daca mail-ul a fost schimbat
            1 => NoContent(),
            // daca mail-ul nu a fost schimbat 
            0 => Ok(),
            _ => BadRequest("A avut loc o eroare. Va rugam incercati mai tarziu")
        };
    }
    

    [HttpPost("changeEmail/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmPersonalDataChanging([FromRoute] string token , [FromBody] ConturiDto updatedDto)
    {
       
        if (token.Length != 100 || token.IsNullOrEmpty())
        {
            return BadRequest("Token is not present");
        }
        
        var response = await _userService.ModifyUserDataIfEmailHasChangedAsync(token, updatedDto);
        // de innoit JWT Token-ul pe endpoint-ul asta
        if (response == 1)
        {
            var username = updatedDto.Username;
            
        }
        
        return response switch
        {
            1 => Ok("Data has been changed succesfully!"),
            -1 => NotFound("No account found with this token / Token expired !"),
            0 => BadRequest("Error when trying to acces the link"),
            _ => BadRequest("General error")
        };
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

    [HttpPut("profile/addresses")]
    [Authorize]
    public async Task<IActionResult> ModifyAddress([FromBody] ModifyAddressDto modifiedAddress)
    {
        var userId = int.Parse(HttpContext.User.FindFirst(claim => claim.Type == "user_id")!.Value);

        var response = await _adressService.ModifyAddress(userId, modifiedAddress.NewAddressData);

        return response switch
        {
            1 => Ok("Address modified succesfully"),
            -3 => NotFound("Address not found in db."),
            _ => BadRequest("Error when deleting the address")
        };
    }

    [HttpGet("admin/emailChanged/{changeRequestId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ChangeEmailByAdmin([FromRoute] string changeRequestId)
    {
        var cacheKey = $"Data_{changeRequestId}";
        
        if (!_cache.TryGetValue(cacheKey, out string? emailData))
        {
            return BadRequest("Invalid or expired link.");
        }
        
        var responseFromEmailChangedByAdmin = await _adminService.EmailChangedByAdmin(emailData! , cacheKey);

        return responseFromEmailChangedByAdmin switch
        {
            1 => Ok("Email verified succesfully!Email has been changed"),
            -1 => NotFound("Exception catched!Error has occured"),
            -2 => BadRequest("Expired link! Request a new one from the administrator!"),
            _ => StatusCode(500, "Unknown server error! Contact administrator.")
        };
    }

    [HttpPost("sendContactEmail")]
    [AllowAnonymous]
    public async Task<IActionResult> SendContactEmail([FromForm] string contactDetails)
    {
        var convertToDto = JsonConvert.DeserializeObject<ContactDetails>(contactDetails);

        if (convertToDto == null)
        {
            return BadRequest("An error happened when converting to DTO");
        }

        var response = await _userService.ContactAdmin(convertToDto);

        return response switch
        {
            1 => Ok("Succesfully sent contact email"),
            -1 => NotFound("An error happened when sending the email"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpGet("settings/{name}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGeneralSettings([FromRoute] string name)
    {
        var response = await _generalSettingsService.GetSpecifiedSetting(name);
        return Ok(response);
    }

    [HttpPost("sendBillOnEmail")]
    [Authorize]
    public async Task<IActionResult> SendBillOnEmail([FromBody] SendBillEmailDto sendingBillOnEmail)
    {
        if (sendingBillOnEmail.Currency != "RON" && sendingBillOnEmail.Currency != "EUR")
        {
            sendingBillOnEmail.Currency = "RON";
        }
        
        Console.WriteLine($"id {sendingBillOnEmail.OrderId} , {sendingBillOnEmail.Email}");

        var response = await _orderService.SendBillOnMail(sendingBillOnEmail.OrderId, sendingBillOnEmail.Email,
            sendingBillOnEmail.Currency);

        return response switch
        {
            1 =>  Ok("Succesfully sent bill on email"),
            -2 => NotFound("Adminul nu a configurat serviciul de generare de factura inca."),
            -1 => BadRequest("Comanda pentru care vreti sa trimiteti factura nu a fost gasita"),
            _ => StatusCode(500, "Internal server error")
        };
    }

    [HttpGet("visualizeBill/{orderId:int:required}/{currency:required}")]
    [Authorize]
    public async Task<IActionResult> VisualizeBill([FromRoute] int orderId,[FromRoute] string currency = "RON")
    {
        var response = await _userService.VisualizeAndDownloadUserBillAsync(orderId, currency);
        return response.Key switch
        {
            1 =>  File(response.Value!, "application/pdf", $"factura_{orderId}.pdf"),
            -2 => NotFound("Adminul nu a configurat serviciul de generare de factura inca."),
            -1 => BadRequest("Comanda pentru care vreti sa vizualizati factura nu a fost gasita"),
            _ => StatusCode(500, "Internal server error")
        };
    }
    
    [HttpPost("getAllPopUps")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopUps()
    {
        var response = await _popUpService.GetAllPopUps();
        return Ok(response);
    }
    
}