using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Services.Helpers.adminHelpers;
using E_Commerce_BackEnd.Services.uAdminService;
using E_Commerce_BackEnd.Services.uProductsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_BackEnd.Controllers
{
    
    [ApiController]
    [EnableCors("AllowVueApp")]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly DocumentProcessing _documentProcessing;

        public AdminController(IAdminService adminService
            , DocumentProcessing documentProcessing)
        {
            _adminService = adminService;
            _documentProcessing = documentProcessing;
           
        }

       
        [HttpGet("login")]
        [Authorize]
        public IActionResult GetAdminLogIn()
        {
            
            return Ok("Authorized");
        }

        [HttpPost("login")]
        [Authorize]
        public async Task<IActionResult> LogInToDashBoard([FromBody] AdminLogInDto adminLogInDto)
        {
            var result = await _adminService.AdminLogIn(adminLogInDto.Key);
            
            if (result == 1)
            {
                var cookieOptions = new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(1)
                };
                
                Response.Cookies.Append("adminLoggedIn" , "1" , cookieOptions);
                return Ok("Succesfully logged in into admin dashboard");
            }

            return Unauthorized("Bad credentials");
        }

        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboardInfo()
        {
            await Task.CompletedTask;
            return Ok("Authorized in dashboard");
        }
        // de terminat functionalitatile pt voucher 
        [HttpGet("products")]
        [Authorize]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _adminService.GetProductsForDtoAdminListing();
           
            if (products is null)
            {
                
                return NoContent();
            }
            
            return Ok(products);
        }

        [HttpGet("product/{codProdus}")]
        [Authorize]
        public async Task<IActionResult> SingleProductPage([FromRoute] string codProdus)
        {
            var product = await _adminService.GetProductForAdminPage(codProdus);
           
            if (product is null)
            {
                return NoContent();
            }

            return Ok(product);
        }
        
        [HttpPost("products/addProducts")]
        [Authorize]
        public async Task<IActionResult> AddListOfProducts([FromForm] ExcelReceiverDto? excelReceiverDto)
        {
            if (excelReceiverDto is null)
            {
                return BadRequest("Excel file is not valid");
            }

            using var stream = new MemoryStream();
            await excelReceiverDto.ExcelFromClient.CopyToAsync(stream);
            stream.Position = 0;

            await _documentProcessing.ReadProductsExcel(stream);
            return Ok();
        }
    }
}