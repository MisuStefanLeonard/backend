using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Services.uProductsService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_BackEnd.Controllers;

[ApiController]
[EnableCors("AllowVueApp")]
[Route("api/product")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }


    [HttpDelete("delete/{codProdus}")]
    [Authorize]

    public async Task<IActionResult> DeleteProduct([FromRoute] string codProdus)
    {
        var deleteProductResponse = await _productService.DeleteProduct(codProdus);

        switch (deleteProductResponse)
        {
            case 1:
                return Ok("Deleted succesfully");
            case -1:
                return NotFound("Product not found");
            case -2:
                return BadRequest("Bad request, rolling back transaction");
        }

        return StatusCode(500, "Server error");
    }

    [HttpDelete("delete/{tipProdus}/{categorieProdus}/{codProdus}")]
    [Authorize]
    public async Task<IActionResult> DeleteTypeOnProduct([FromRoute] string codProdus,
                    [FromRoute]string tipProdus,[FromRoute] string categorieProdus)
    {
        var responseToDeleteTypeOnProduct = await _productService
            .DeleteTypeOnProduct(codProdus, tipProdus, categorieProdus);

        return responseToDeleteTypeOnProduct switch
        {
            1 => Ok("Deleted type on product succesfully"),
            0 => NotFound("An error occured when querying the database"),
            -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
            _ => StatusCode(500, "Server error")
        };
    }
    
    [HttpDelete("delete/{codProdus}/{lungime}/{latime}/{pret}/{recomandarePat}")]
    [Authorize]
    public async Task<IActionResult> DeleteDimensionOnProduct([FromRoute] string codProdus, [FromRoute]string lungime,
        [FromRoute] string latime, [FromRoute] string pret, [FromRoute] string recomandarePat)
    {
        var responseToDeleteDimensionOnProduct = await _productService
            .DeleteDimensionOnProduct(codProdus, lungime, latime,pret,recomandarePat);

        return responseToDeleteDimensionOnProduct switch
        {
            1 => Ok("Deleted dimension on product succesfully"),
            0 => NotFound("An error occured when querying the database"),
            -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
            _ => StatusCode(500, "Server error")
        };
    }
    
    [HttpDelete("delete/{codProdus}/{numeCuloare}/{codCuloare}/color")]
    [Authorize]
    public async Task<IActionResult> DeleteDimensionOnProduct([FromRoute] string codProdus, 
        [FromRoute]string numeCuloare,[FromRoute] string codCuloare)
    {
        var responseToDeleteColorOnProduct = await _productService
            .DeleteColorOnProduct(codProdus, numeCuloare, codCuloare);

        return responseToDeleteColorOnProduct switch
        {
            1 => Ok("Deleted color on product succesfully"),
            0 => NotFound("An error occured when querying the database"),
            -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpDelete("delete/{codProdus}/{numeCuloare}/{codCuloare}/{caleImagine}/{fisierInBucket}/image")]
    [Authorize]
    public async Task<IActionResult> DeleteImageOnProduct([FromRoute] string codProdus,
        [FromRoute] string numeCuloare, [FromRoute] string codCuloare, [FromRoute] string caleImagine,
        [FromRoute] string fisierInBucket)
    {
        var responseToDeleteImageOnProduct = await _productService
            .DeleteImageOnProduct(codProdus, numeCuloare, codCuloare,caleImagine,fisierInBucket);

        return responseToDeleteImageOnProduct switch
        {
            1 => Ok("Deleted image on product succesfully"),
            0 => NotFound("An error occured when querying the database"),
            -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpDelete("delete/{codProdus}/{codVoucher}")]
    [Authorize]
    public async Task<IActionResult> DeleteVoucherOnProduct([FromRoute] string codProdus, [FromRoute] string codVoucher)
    {
        var responseToDeleteVoucherOnProduct = await _productService
            .DeleteVoucherOnProduct(codProdus, codVoucher);

        return responseToDeleteVoucherOnProduct switch
        {
            1 => Ok("Deleted voucher on product succesfully"),
            0 => NotFound("An error occured when querying the database"),
            -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
            _ => StatusCode(500, "Server error")
        };
    }

    [HttpGet("getProductTypes")]
    [Authorize]
    public async Task<IActionResult> GetProductTypes()
    {
        var responseForProductOptions = await _productService.GetProductTypes();

        if (responseForProductOptions is null)
        {
            return NoContent();
        }

        return Ok(responseForProductOptions);
    }

    // [HttpPut("save/{codProdus}")]
    // [Authorize]
    // public async Task<IActionResult> SaveModifiedProduct([FromBody] ProduseDtoForAdminModification modifiedProduct)
    // {
    //     
    // }

    
}