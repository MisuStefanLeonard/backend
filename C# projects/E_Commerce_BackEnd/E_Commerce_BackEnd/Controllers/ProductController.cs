using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.Services.uReviewService;
using E_Commerce_BackEnd.Services.uSeturiService;
using E_Commerce_BackEnd.Services.uShoppingCartService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Sqids;


namespace E_Commerce_BackEnd.Controllers;

[ApiController]
[EnableCors("AllowVueApp")]
[Route("api/product")]
public class ProductController : ControllerBase
{

    private readonly IProductService _productService;
    private readonly IReviewService _reviewService;
    private readonly ISeturiService _seturiService;
    private readonly SqidsEncoder<int> _sqidsEncoder;
    private readonly ICartService _cartService;
    private readonly ITokenService _tokenService;


    public ProductController(IProductService productService, IReviewService reviewService, SqidsEncoder<int> sqidsEncoder, ISeturiService seturiService, ICartService cartService, ITokenService tokenService)
    {
        _productService = productService;
        _reviewService = reviewService;
        _sqidsEncoder = sqidsEncoder;
        _seturiService = seturiService;
        _cartService = cartService;
        _tokenService = tokenService;
    }

    [HttpGet("paginated/{pageNumber:int?}/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductsPerPage([FromRoute] int? pageNumber ,[FromQuery] string? productTypes,
        [FromQuery] string? productColors, [FromQuery] string? productDimensions, [FromQuery] string? productPrice, 
        [FromQuery] bool? productReverseFace , [FromRoute] string currency)
    {
        
        List<string>? listOfProductTypes = null;
        List<string>? listOfProductColors = null;
        List<string>? listOfProductDimensions = null;
        List<decimal>? listOfProductPrices = null;
       

        if (!productTypes.IsNullOrEmpty())
        {
            listOfProductTypes = productTypes!.Split(",").ToList();
        }
        if (!productColors.IsNullOrEmpty())
        {
            listOfProductColors = productColors!.Split(",").ToList();
        } 
        if(!productDimensions.IsNullOrEmpty())
        {
            listOfProductDimensions = productDimensions!.Split(",").ToList();
        } 
        if(!productPrice.IsNullOrEmpty())
        {
            listOfProductPrices = productPrice!.Split(",").Select(Convert.ToDecimal).ToList();
        } 
        
        var productsPerPage = await _productService.GetProductsForUsers(pageNumber, listOfProductTypes, listOfProductColors
                            ,listOfProductDimensions,listOfProductPrices,productReverseFace , currency.ToUpper());
        
        return Ok(productsPerPage);
    }
    
    [HttpGet("filterOptions/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFilterOptions([FromRoute] string currency)
    {
        var filterOptions = await _productService.FilterOptions(currency);
        return Ok(filterOptions);
    }

    [HttpGet("{codProdus}/{tipProdus}/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductPage([FromRoute] string codProdus , [FromRoute] string tipProdus , [FromRoute] string currency)
    {
        var productData = await _productService.GetProductPage(codProdus, tipProdus, currency.ToUpper());

        return productData switch
        {
            { Key: 1, Value: not null } => Ok(productData.Value),
            { Key: 1, Value: null } => NotFound("Product has been deleted"),
            { Key: 0, Value: null } => BadRequest("General error occured"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpPost("postReview")]
    [Authorize]
    public async Task<IActionResult> PostReview([FromForm] string reviewInfo)
    {
       
        var jsonToDto = JsonConvert.DeserializeObject<ReviewReceivedDto>(reviewInfo);

        if (!Request.Cookies.TryGetValue("JWTToken", out var token) || !Request.Cookies.TryGetValue("session_tok", out var refreshToken)) 
            return StatusCode(500, "Server error");
        // de adaugat decodarea pe encodedIdSet
        var responseFromReviewPosting = await _reviewService.PostReview(jsonToDto!, token,refreshToken);
        return responseFromReviewPosting.Key switch
        {
            1 => Ok(responseFromReviewPosting),
            0 => NotFound("Product has not been found/Account has not been found"),
            -1 => BadRequest("General error occured"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpGet("sets/paginated/{pageNumber:int?}/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSetsPerPage([FromRoute] int? pageNumber ,[FromQuery] string? productTypes,
         [FromQuery] string? productPrice, [FromRoute] string currency, [FromQuery] string? productName)
    {

        List<string>? listOfProductTypes = null;
        List<decimal>? listOfProductPrices = null;

        if (!productTypes.IsNullOrEmpty())
        {
            listOfProductTypes = productTypes!.Split(",").ToList();
        }
        
        if(!productPrice.IsNullOrEmpty())
        {
            listOfProductPrices = productPrice!.Split(",").Select(Convert.ToDecimal).ToList();
        }

        var setsPerPage = await _seturiService.GetSetsForUsers(pageNumber, listOfProductTypes,
            listOfProductPrices,productName ,currency);
        
        return Ok(setsPerPage);
    }
    
    
    
    [HttpGet("set/{encodedIdSet}/{numeSet}/{currency}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSetPage([FromRoute] string encodedIdSet, [FromRoute] string numeSet,
        [FromRoute] string currency)
    {
        if (_sqidsEncoder.Decode(encodedIdSet) is [var decodedId]
            && encodedIdSet == _sqidsEncoder.Encode(decodedId))
        {
            var setData = await _seturiService.GetSetForUser(decodedId, numeSet, currency);

            return setData switch
            {
                { Key: 1, Value: not null } => Ok(setData.Value),
                { Key: 1, Value: null } => NotFound("Set has been deleted"),
                { Key: 0, Value: null } => BadRequest("General error occured"),
                _ => StatusCode(500, "Server error")
            };
        }

        return StatusCode(500, "Decoded id not found");
    }

    [HttpPost("cart/add")]
    [AllowAnonymous]
    public async Task<IActionResult> AddOrUpdateCart([FromForm] string? cartItem , [FromForm] string? setItems)
    {
        var cartItemToDto = JsonConvert.DeserializeObject<ProductOnCartDto>(cartItem!); // can be null
        var setItemsToDto = JsonConvert.DeserializeObject<SetOnCartDto>(setItems!); // can be null
        var getSessionId = Request.Cookies["ASP.NET_COOKIE_cartSession"];
        var emptyGuid = Guid.Empty;
        if (getSessionId != null)
        {
            emptyGuid = Guid.Parse(getSessionId.Split('|')[0]);
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
        }
        
        
        int? userId = null;
        var idClaim = HttpContext.User.FindFirst("user_id");
        if (idClaim != null)
        {
            userId = int.Parse(idClaim.Value);
        }
        
        var responseFromCartAdd = await _cartService.AddOrUpdateCart(emptyGuid,userId, setItemsToDto , cartItemToDto);

        return responseFromCartAdd switch
        {
            {Key: 1} => Ok("Succesfully added to cart"),
            {Key: 2} => NoContent(), // quantity incremented
            {Key: 0} => NotFound("Encoded id of the set could not be decoded"),
            {Key: -1} => BadRequest("Exception thrown in the addCart function"),
            _ => StatusCode(500, "General error occured")
        };

    }
    
    [HttpPost("cart/delete")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteFromCart([FromForm] string productData)
    {
        var convertToJson =  JsonConvert.DeserializeObject<UpdateQuantityInfo>(productData);

        if (convertToJson == null)
        {
            return BadRequest("JSON CANNOT BE CONVERTED. CHECK LOGS.");
        }
        
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var sessionId))
        {
            convertToJson.SessionId = Guid.Parse(sessionId.Split('|')[0]);
            
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
                    convertToJson.IdCont = int.Parse(HttpContext.User.FindFirst("user_id")!.Value);
                    convertToJson.SessionId = Guid.Empty;
                }
            }
        }
        else
        {
            return BadRequest("Refresh page again! Session id missing");
        }


        var responseFromCartDelete = await _cartService.DeleteFromCart(convertToJson);

        return responseFromCartDelete switch
        {
            -2 => NotFound("Product/Bundle does not exist anymore"),
            -1 => BadRequest("Exception thrown.Check logs "),
            1 => Ok("Succesfully deleted the product"),
            _ => StatusCode(500 , "General error occured. Maybe server error")
        };
        
        
        
        

    }

    [HttpGet("mostViewedProducts/{currency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMostViewedProducts([FromRoute] string currency)
    {
        var productsList = await _productService.GetMostViewedProducts(currency);
        return Ok(productsList);
    }

    [HttpGet("syncCartOnCheckout/{currency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckCartOnCheckout([FromRoute] string currency)
    {
        Guid sessionIdentifier;
        DateTime dateTimeSinceCookieWasAdded;
        var currentDateTime = DateTime.UtcNow;
        var id = 0;
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var sessionId))
        {
            sessionIdentifier = Guid.Parse(sessionId.Split('|')[0]);
            dateTimeSinceCookieWasAdded = DateTime.Parse(sessionId.Split('|')[1]);
            if ((currentDateTime - dateTimeSinceCookieWasAdded).TotalHours >= 60)
            {
                /* Cart session renewal for checkout endpoint.  */
                var refreshCartSession = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(3)
                };
                
                Response.Cookies.Append("ASP.NET_COOKIE_cartSession",sessionId.Split('|')[0]+$"|{currentDateTime}",refreshCartSession);
                /* Cart session renewal for checkout endpoint.  */
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
        
        var response = await _cartService.CheckCartAtCheckout(id == 0 ? null : id , sessionIdentifier,dateTimeSinceCookieWasAdded,currentDateTime.AddDays(3),currency);
       
        return response.Item1 switch
        {
            -2 => NotFound("Cart empty. Abort"),
            1 => Ok(/*updated items*/response.Item2),
            _ => BadRequest("ERROR THROWN. CHECK LOGS")
        };


    }
    
    // de terminat pe front-end afisarea produselor in cart!
    [HttpGet("cart/{currency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCartItems([FromRoute] string currency)
    {
        var sessionId = Guid.Empty;
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var guid) )
        {
            sessionId = Guid.Parse(guid.Split('|')[0]);
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
        }
        
       
        var userId = 0;
        var getUserId = HttpContext.User.FindFirst("user_id");
        if (getUserId != null)
        {
            userId = int.Parse(getUserId.Value);
        }
      
        if (userId == 0 && sessionId == Guid.Empty)
        {
            return BadRequest("Refresh page and try again");
        }

        var response = await _cartService.GetCartItems(userId != 0 ? userId : null, sessionId , currency);

        return Ok(response);
    }

    [HttpPost("modify/quantity")]
    [AllowAnonymous]
    public async Task<IActionResult> ModifyQuantity([FromForm] string productData)
    {
        var convertToJson =  JsonConvert.DeserializeObject<UpdateQuantityInfo>(productData);

        if (convertToJson == null)
        {
            return BadRequest("JSON CANNOT BE CONVERTED. CHECK LOGS.");
        }
        
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var sessionId))
        {
            convertToJson.SessionId = Guid.Parse(sessionId.Split('|')[0]);
            
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
                convertToJson.IdCont = int.Parse(HttpContext.User.FindFirst("user_id")!.Value);
                convertToJson.SessionId = Guid.Empty;
            }
        }
        else
        {
            return BadRequest("Refresh page again! Session id missing");
        }


        var response = await _cartService.ModifyQuantity(convertToJson);

        return response switch
        {
            -2 => NotFound("Product/Bundle does not exist anymore"),
            -1 => BadRequest("Exception thrown.Check logs "),
            1 => Ok("Succesfully incremented/decremented quantity"),
            _ => StatusCode(500 , "General error occured. Maybe server error")
        };
    }

    
    [HttpPost("applyVoucher")]
    [AllowAnonymous]
    public async Task<IActionResult> ApplyVoucher([FromBody] CheckVoucher voucherData)
    {
        var sessionId = Guid.Empty;
        if (Request.Cookies.TryGetValue("ASP.NET_COOKIE_cartSession", out var guid))
        {
            sessionId = Guid.Parse(guid.Split('|')[0]);
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
        }
       
        var userId = 0;
        var getUserId = HttpContext.User.FindFirst("user_id");
        if (getUserId != null)
        {
            userId = int.Parse(getUserId.Value);
        }
      
        if (userId == 0 && sessionId == Guid.Empty)
        {
            return BadRequest("Refresh page and try again");
        }
        
        var (key, data) = await _cartService.GetVoucherInfo(userId != 0 ? userId : null ,voucherData);

        return key switch
        {
            1 => Ok(data),
            0 => NotFound("Invalid voucher/Expired voucher"),
            -2 => BadRequest("Voucher already used!"),
            -1 => NoContent(), // account not found
            _ => StatusCode(500 , "Server error")
        };
    }

    [HttpGet("productTypesAndCategories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductTypesAndCategoriesForUserDisplay()
    {
        var response = await _productService.GetProductTypesAndSubCategories();
        return Ok(response);
    }

    [HttpGet("limitedEditionProducts/{currentCurrency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLimitedEditionProducts([FromRoute] string currentCurrency)
    {
        var response = await _productService.GetProductsThatAreLimitedEdition(currentCurrency);
        return Ok(response);
    }
    
    [HttpGet("newProducts/{currentCurrency:required}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNewProducts([FromRoute] string currentCurrency)
    {
        var response = await _productService.GetProductsThatAreNew(currentCurrency);
        return Ok(response);
    }

    


}