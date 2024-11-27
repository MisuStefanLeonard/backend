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
        Console.WriteLine($"product widhts : {productDimensions} ");
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

        if (!Request.Cookies.TryGetValue("JWTToken", out var token)) 
            return StatusCode(500, "Server error");
        // de adaugat decodarea pe encodedIdSet
        var responseFromReviewPosting = await _reviewService.PostReview(jsonToDto!, token);
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
    [Authorize]
    public async Task<IActionResult> AddOrUpdateCart([FromForm] string? cartItem , [FromForm] string? setItems)
    {
        var cartItemToDto = JsonConvert.DeserializeObject<ProductOnCartDto>(cartItem!); // can be null
        var setItemsToDto = JsonConvert.DeserializeObject<SetOnCartDto>(setItems!); // can be null
        

        var currentUserClaims = HttpContext.User;
        var userId = int.Parse(currentUserClaims.FindFirst("user_id")!.Value);

        var responseFromCartAdd = await _cartService.AddOrUpdateCart(userId, setItemsToDto , cartItemToDto);

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
    [Authorize]
    public async Task<IActionResult> DeleteFromCart([FromForm] string? cartItemToDelete , [FromForm] string? setItemsToDelete)
    {
        var cartItemToDeleteToDto = JsonConvert.DeserializeObject<ProductOnCartDto>(cartItemToDelete!); // can be null
        var setItemsToDeleteToDto = JsonConvert.DeserializeObject<SetOnCartDto>(setItemsToDelete!); // can be null

        var currentUserClaims = HttpContext.User;
        var userId = int.Parse(currentUserClaims.FindFirst("user_id")!.Value);

        var responseFromCartDelete = await _cartService.DeleteFromCart(userId, setItemsToDeleteToDto,cartItemToDeleteToDto);

        return responseFromCartDelete switch
        {
            1 => Ok("Succesfully removed item from cart"),
            2 => NoContent(), // quantity decremented
            -2 => NotFound("Product not found"),
            -1 => BadRequest("Exception thrown in the deleteFromCart function"),
            _ => StatusCode(500, "General error occured")
        };

    }
    
    
}