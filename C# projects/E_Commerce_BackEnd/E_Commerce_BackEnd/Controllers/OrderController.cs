using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.OrdersDto;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.uOrdersService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace E_Commerce_BackEnd.Controllers;


[ApiController]
[EnableCors("AllowVueApp")]
[Route("api/order")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITokenService _tokenService;

    public OrderController(IOrderService orderService, ITokenService tokenService)
    {
        _orderService = orderService;
        _tokenService = tokenService;
    }

    [HttpGet("client/orders/{currency:required}")]
    [Authorize]
    public async Task<IActionResult> GetClientOrders([FromRoute] string currency)
    {
        if (currency is not ("RON" or "EUR"))
        {
            return BadRequest("Invalid currency");
        }
        
        var userId = 0;
        var getUserId = HttpContext.User.FindFirst("user_id");
        if (getUserId != null)
        {
            userId = int.Parse(getUserId.Value);
        }
        else
        {
            return NotFound("Invalid JWT Token. Claim not found");
        }

        var response = await _orderService.GetClientOrders(userId,currency);

        return Ok(response);
    }


    [HttpPost("place_order/{currency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> PlaceOrder([FromRoute] string currency , [FromForm] string orderDto)
    {
        if (currency is not ("RON" or "EUR"))
        {
            return BadRequest("Invalid currency");
        }

        if (currency == "EUR")
        {
            currency = "RON";
        }

        var convertToPlaceOrderDto = JsonConvert.DeserializeObject<PlaceOrderDto>(orderDto);

        if (convertToPlaceOrderDto == null)
        {
            return BadRequest("DTO cannot be converted. Contact administrator");
        }
        
        Guid sessionIdentifier;
        var currentDateTime = DateTime.UtcNow;
        var id = 0;
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var sessionId))
        {
            sessionIdentifier = Guid.Parse(sessionId.Split('|')[0]);
            var dateTimeSinceCookieWasAdded = DateTime.Parse(sessionId.Split('|')[1]);
            if ((currentDateTime - dateTimeSinceCookieWasAdded).TotalHours >= 60)
            {
                return NotFound("Session about to expire. Do not enter this directly . Go to the checkout session");
            }
            
            if (Request.Cookies.ContainsKey("userLoggedIn") && Request.Cookies.ContainsKey("session_tok"))
            {
                // jwt expired 
                if(!Request.Cookies.ContainsKey("JWTToken"))
                {
                    if (Request.Cookies.TryGetValue("session_tok", out var refreshToken))
                    {
                        var getValidToken = await _tokenService.TokenValidation("refresh", refreshToken);
                        HttpContext.User = getValidToken.Item1!;
                        var cookieOptions = new CookieOptions()
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddMinutes(15)

                        };
                        var generateNewJwt = await _tokenService.GenerateJwtAccesToken(getValidToken.Item2!);
                        Response.Cookies.Append("JWTToken" , generateNewJwt , cookieOptions);
                     
                      
                    }
                }
                id = int.Parse(HttpContext.User.FindFirst("user_id")!.Value);
                sessionIdentifier = Guid.Empty;
            }
            
        }
        else
        {
            return BadRequest("Refresh page again! Session id missing");
        }


        var (intResponse, stringValue) = await _orderService.PlaceOrder(id == 0 ? null : id, sessionIdentifier, convertToPlaceOrderDto, currency);


        return intResponse switch
        {
            -2 => NotFound("Error thrown. Cancelling transaction"),
            -3 => NoContent(), // PAYMENT REJECTED.
            -1 => BadRequest($"Voucher not found"),
            1 => Ok($"Token {stringValue}"),
            _ => StatusCode(500, "Server error")
        };
    }
    
    
    // [HttpGet]
    // [AllowAnonymous]
}