using System.Text;
using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.Accounts;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.VouchereDtos;
using E_Commerce_BackEnd.Models.Enums;
using E_Commerce_BackEnd.Services.Helpers.adminHelpers;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret;
using E_Commerce_BackEnd.Services.Helpers.AWS_Secret.AWSBucket_CRUD;
using E_Commerce_BackEnd.Services.uAdminService;
using E_Commerce_BackEnd.Services.uGeneralService;
using E_Commerce_BackEnd.Services.uInelePrindereService;
using E_Commerce_BackEnd.Services.uManopereService;
using E_Commerce_BackEnd.Services.uProductsService;
using E_Commerce_BackEnd.Services.uService;
using E_Commerce_BackEnd.Services.uSeturiService;
using E_Commerce_BackEnd.Services.uTipuriGalerieService;
using E_Commerce_BackEnd.Services.uTipuriLinieService;
using E_Commerce_BackEnd.Services.uVoucherService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Sqids;

namespace E_Commerce_BackEnd.Controllers
{
    
    [ApiController]
    [EnableCors("AllowVueApp")]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ISeturiService _seturiService;
        private readonly IProductService _productService;
        private readonly IInelePrindereService _inelePrindereService;
        private readonly ITipuriGalerieService _tipuriGalerieService;
        private readonly ITipuriLinieService _tipuriLinieService;
        private readonly IVoucherService _voucherService;
        private readonly IUserService _userService;
        private readonly DocumentProcessing _documentProcessing;
        private readonly IBucketAcces _bucketAcces;
        private readonly IManopereService _manopereService;
        private readonly IGeneralSettingsService _generalSettingsService;
        private readonly SqidsEncoder<int> _sqidsEncoder;

        public AdminController(IAdminService adminService
            , DocumentProcessing documentProcessing, IProductService productService, ISeturiService seturiService, 
             SqidsEncoder<int> sqidsEncoder, 
            IInelePrindereService inelePrindereService, ITipuriGalerieService tipuriGalerieService, 
            ITipuriLinieService tipuriLinieService, IUserService userService, IVoucherService voucherService, IManopereService manopereService, IGeneralSettingsService generalSettingsService
            , IBucketAcces bucketAcces)
        {
            _adminService = adminService;
            _documentProcessing = documentProcessing;
            _productService = productService;
            _seturiService = seturiService;
            _sqidsEncoder = sqidsEncoder;
            _inelePrindereService = inelePrindereService;
            _tipuriGalerieService = tipuriGalerieService;
            _tipuriLinieService = tipuriLinieService;
            _userService = userService;
            _voucherService = voucherService;
            _manopereService = manopereService;
            _generalSettingsService = generalSettingsService;
            _bucketAcces = bucketAcces;
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

            if (result != 1) return Unauthorized("Bad credentials");
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            
            var getSecretHeader = await TokenService.GetSecret("prod/texx.ro/admin-header");
            var bytesSecret = Encoding.UTF8.GetBytes(getSecretHeader);
            Response.Cookies.Append("ASP_NET_ADMIN_SESSION" , Convert.ToBase64String(bytesSecret) , cookieOptions);
            Response.Cookies.Append("adminLoggedIn" , "1" , cookieOptions);
            return Ok("Succesfully logged in into admin dashboard");

        }

        [HttpGet("dashboard/{lowerInterval}/{upperInterval}")]
        [Authorize]
        public async Task<IActionResult> GetDashboardInfo([FromRoute] string? lowerInterval , [FromRoute] string? upperInterval)
        {
            if (lowerInterval == "null" && upperInterval == "null")
            {
                lowerInterval = null;
                upperInterval = null;
            }

            if (lowerInterval != null && upperInterval != null)
            {
                var lowerIntervalToDateTime = DateTime.Parse(lowerInterval);
                var upperIntervalToDateTime = DateTime.Parse(upperInterval);
                var dashBoardData = await _adminService.GetMainDashboardData(lowerIntervalToDateTime,upperIntervalToDateTime);
                return Ok(dashBoardData);
            }
            else
            {
                var dashBoardData = await _adminService.GetMainDashboardData(null,null);
                return Ok(dashBoardData);
            }
            
    
           
        }

        [HttpGet("dashboard/GA/{lowerInterval}/{upperInterval}")]
        [Authorize]
        public async Task<IActionResult> GetGaInfo([FromRoute] string? lowerInterval , [FromRoute] string? upperInterval)
        {
            if (lowerInterval == "null" && upperInterval == "null")
            {
                lowerInterval = null;
                upperInterval = null;
            }
            
            if (lowerInterval != null && upperInterval != null)
            {
                var gaData = await _adminService.GetGoogleAnalyticsData(lowerInterval,upperInterval);
                return Ok(gaData);
            }
            else
            {
                 var gaData = await _adminService.GetGoogleAnalyticsData(null,null);
                 return Ok(gaData);
            }
           

          
        }
     
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

            var response = await _documentProcessing.ReadProductsExcel(stream);
            if (!response.Success)
            {
                return BadRequest(response.Message);
            }
            
            return Ok(response.Message);
            
        }
        
        [HttpDelete("delete/{codProdus}")]
        [Authorize]

        public async Task<IActionResult> DeleteProduct([FromRoute] string codProdus)
        {
            var deleteProductResponse = await _productService.DeleteProduct(codProdus);

            return deleteProductResponse switch
            {
                1 => Ok("Deleted succesfully"),
                -1 => NotFound("Product not found"),
                -2 => BadRequest("Bad request, rolling back transaction"),
                -3 => StatusCode(515 , "Product is being bought.Cannot modify/delete"),
                _ => StatusCode(500, "Server error")
            };
        }

        [HttpDelete("delete/{categorieProdus}/{codProdus}/type")]
        [Authorize]
        public async Task<IActionResult> DeleteTypeOnProduct([FromRoute] string codProdus, [FromRoute] string categorieProdus)
        {
            var responseToDeleteTypeOnProduct = await _productService
                .DeleteTypeOnProduct(codProdus, categorieProdus);

            return responseToDeleteTypeOnProduct switch
            {
                1 => Ok("Deleted type on product succesfully"),
                0 => NotFound("An error occured when querying the database"),
                -1 => BadRequest("An fatal error occured when querying the database! Rolling back transaction"),
                -3 => StatusCode(515 , "Produsul este blocat ."),
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
                -3 => StatusCode(515 , "Produsul este blocat ."),
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
                -3 => StatusCode(515 , "Produsul este blocat ."),
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
                -3 => StatusCode(515 , "Produsul este blocat ."),
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

        [HttpGet("getProductCodesAndNames")]
        [Authorize]
        public async Task<IActionResult> CheckProductCode()
        {
            var codesAndNames = await _productService.GetProductCodesAndNames();

            if (codesAndNames is null)
            {
                return NoContent();
            }
            
            return Ok(codesAndNames);
        }

        [HttpPut("save/{codProdus}")]
        [Authorize]
        public async Task<IActionResult> SaveModifiedProduct([FromForm] IFormFileCollection images, [FromForm] string productDto,
            [FromRoute] string codProdus)
        {
            try
            {
                // Deserialize the product DTO
                var modifiedProduct = JsonConvert.DeserializeObject<ProduseDtoForAdminModification>(productDto);

               
                if (modifiedProduct is null)
                {
                    return NotFound("PRODUCT MODIFICATION IS NULL");
                }
                
                var response = await _productService.UpdateProduct(modifiedProduct,images);

                return response.All(x => x == 0) ? StatusCode(515, "Product locked!Cannot modify") : Ok(response); // if it returns [] , product is locked 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPut("toggleActivationState/{codProdus}/{activationState:bool}")]
        [Authorize]
        public async Task<IActionResult> ToggleProductActivationState([FromRoute] string codProdus , [FromRoute] bool activationState)
        {
            var responseFromTogglingActivationState = await 
                _productService.ToggleActivationStateInShop(codProdus, activationState);

            return responseFromTogglingActivationState switch
            {
                1 => Ok("Succesfully toggled the state"),
                -3 => StatusCode(515, "Produsul este blocat ."),
                _ => BadRequest("Something happened when toggling the state of the product")
            };
        }

        [HttpPut("deleteSelectedProducts")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedProducts([FromBody] BulkOperationsDto toDelete)
        {
           
            var responseFromBulkDeletion = await _productService.DeleteSelectedProducts(toDelete);

            return responseFromBulkDeletion switch
            {
                1 => Ok("Succesfully deleted the selected products"),
                -3 => StatusCode(515, " Cannot delete . product is being bought"),
                _ => BadRequest("An error happened. Please refresh")
            };
        }
        
        [HttpPut("activateSelectedProducts")]
        [Authorize]
        public async Task<IActionResult> ActivateSelectedProducts([FromBody] BulkOperationsDto toActivate)
        {
           
            var responseFromBulkActivation = await _productService.ActivateSelectedProducts(toActivate);

            switch (responseFromBulkActivation)
            {
                case 1:
                    return Ok("Succesfully activated the selected products");
                case -3:
                    StatusCode(515, " Cannot delete . product is being bought");
                    break;
            }

            return BadRequest("An error happened. Please refresh");
        }
      
        
        [HttpGet("seturi")]
        [Authorize]
        public async Task<IActionResult> GetProductsSets()
        {
            var productsSets = await _seturiService.GetProductSets();
            
            return Ok(productsSets);
        }
        
        [HttpGet("seturi/nume")]
        [Authorize]
        public async Task<IActionResult> GetSeturiNames()
        {
            var seturiNames = await _seturiService.GetSetNames();
            
            return Ok(seturiNames);
        }
        
        [HttpGet("set/{encodedIdSetDto}")]
        [Authorize]
        public async Task<IActionResult> GetSetById([FromRoute] string encodedIdSetDto)
        {
            if (_sqidsEncoder.Decode(encodedIdSetDto) is [var decodedId]
                && encodedIdSetDto == _sqidsEncoder.Encode(decodedId))
            {
                var setById = await _seturiService.GetSetPage(decodedId);
                
                return Ok(setById);
            }
           
            return StatusCode(555 , "Invalid decoded set ID ");
           
        }

        [HttpPut("set/activate/{encodedIdSetDto}/{activationState:bool}")]
        [Authorize]
        public async Task<IActionResult> ModifyActivationStateOnSet([FromRoute] string encodedIdSetDto,
            [FromRoute] bool activationState)
        {
            if (_sqidsEncoder.Decode(encodedIdSetDto) is [var decodedId]
                && encodedIdSetDto == _sqidsEncoder.Encode(decodedId))
            {
                Console.WriteLine($"activ state : {activationState} decoded id : {decodedId}");
                var responseState = await _seturiService.ActivateSet(decodedId, activationState);

                return responseState switch
                {
                    1 => Ok("Succesful set deletion"), // succesfull
                    -1 => NotFound("Empty list of set"), // empty sets
                    0 => BadRequest("General exception thrown"), // exception thrown
                    _ => StatusCode(500, "General error!Contact admin")
                };
            }
           
            return StatusCode(555 , "Invalid decoded set ID ");
        }
        
        [HttpPut("set/delete/{encodedIdSetDto}")]
        [Authorize]
        public async Task<IActionResult> DeleteSet([FromRoute] string encodedIdSetDto)
        {
            if (_sqidsEncoder.Decode(encodedIdSetDto) is [var decodedId]
                && encodedIdSetDto == _sqidsEncoder.Encode(decodedId))
            {
                var responseState = await _seturiService.DeleteSet(decodedId);

                return responseState switch
                {
                    1 => Ok("Succesful set deletion"), // succesfull
                    -1 => NotFound("Empty list of set"), // empty sets
                    -3 => StatusCode(515 , "Setul nu poate fi sters. Un client este la platirea comenzii cu acest produs"),
                    0 => BadRequest("General exception thrown"), // exception thrown
                    _ => StatusCode(500, "General error!Contact admin")
                };
            }
           
            return StatusCode(555 , "Invalid decoded set ID ");
        }
        
        
        [HttpPut("set/activateBulk")]
        [Authorize]
        public async Task<IActionResult> ActivateSelectedSets([FromBody] BulkOperationsDto toActivate)
        {
           
            var responseFromBulkActivationSets = await _seturiService.ActivateBulkSets(toActivate);

            return responseFromBulkActivationSets switch
            {
                1 => Ok("Succesfully activated the selected sets"),
                -3 => StatusCode(515 , "Setul nu poate fi activat. Un client este la platirea comenzii cu acest produs"),
                -1 => BadRequest("General error thrown"),
                _ => StatusCode(500, "Server error")
            };
        }
        
        [HttpPut("set/deleteBulk")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedSets([FromBody] BulkOperationsDto toDelete)
        {
           
            var responseFromBulkDeletionSets = await _seturiService.DeleteBulkSets(toDelete);

            return responseFromBulkDeletionSets switch
            {
                1 => Ok("Succesfully deleted the selected sets"),
                -3 => StatusCode(515 , "Setul nu poate fi sters. Un client este la platirea comenzii cu acest produs"),
                -1 => BadRequest("General error thrown"),
                _ => StatusCode(500, "Server error")
            };
        }

        [HttpDelete("set/delete/{encodedIdSetDto}/{idProdus:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteProductOnSet([FromRoute] string encodedIdSetDto, [FromRoute] int idProdus)
        {
            if (_sqidsEncoder.Decode(encodedIdSetDto) is [var decodedId]
                && encodedIdSetDto == _sqidsEncoder.Encode(decodedId))
            {
                var deleteResponse = await _seturiService.DeleteProductFromSet(decodedId, idProdus);

                return deleteResponse switch
                {
                    1 => Ok("Succesfull deletion"), // succesfull
                    -2 => NotFound("Empty list of produse / seturi"), // empty products/sets
                    -3 => StatusCode(515 , "Setul nu poate fi sters. Un client este la platirea comenzii cu acest produs"),
                    -1 => BadRequest("General exception thrown"), // exception thrown
                    _ => StatusCode(500, "General error!Contact admin")
                };
            }
            return StatusCode(555 , "Invalid decoded set ID ");

        }

        [HttpPut("set/{encodedIdSet}/update")]
        [Authorize]
        public async Task<IActionResult> ModifySet([FromForm] string modifiedSet, [FromRoute] string encodedIdSet)
        {
            if (_sqidsEncoder.Decode(encodedIdSet) is [var decodedId]
                && encodedIdSet == _sqidsEncoder.Encode(decodedId))
            {
                var modifiedSetToDto = JsonConvert.DeserializeObject<SetModificationDto>(modifiedSet);

                if (modifiedSetToDto is null)
                {
                    return BadRequest("Invalid deserializing from the JSON to Dto");
                }

                var updateResponse = await _seturiService.AddOrUpdateSet(modifiedSetToDto, decodedId, false);

                return updateResponse switch
                {
                    1 => Ok("Succesfully modfied the set"),
                    -3 => StatusCode(515,
                        "Setul nu poate fi modificat. Un client este la platirea comenzii cu acest produs"),
                    _ => BadRequest("An error occured when updating the set")
                };
            }
            return StatusCode(555 , "Invalid decoded set ID ");
        }
        
        [HttpPost("set/add")]
        [Authorize]
        public async Task<IActionResult> ModifySet([FromForm] string newSet)
        {
            
            var newSetToDto = JsonConvert.DeserializeObject<SetModificationDto>(newSet);
            
            if (newSetToDto is null)
            {
                return BadRequest("Invalid deserializing from the JSON to Dto");
            }

            var updateResponse = await _seturiService.AddOrUpdateSet(newSetToDto, 0, true);

            if (updateResponse == 1)
            {
                return Ok("Succesfully added new set");
            }
           
            return BadRequest("An error occured when adding the set");
                
            
        }

        [HttpGet("produse/coduriProduse")]
        [Authorize]
        public async Task<IActionResult> GetProductCodes()
        {
            var productCodes = await _seturiService.GetProductCodes();

            return Ok(productCodes);
        }

        [HttpGet("produs/{codProdus}/data")]
        [Authorize]
        public async Task<IActionResult> GetProductData([FromRoute] string codProdus)
        {
            var data = await _seturiService.GetProductDataForSetAdd(codProdus);

            return Ok(data);
        }
        
        
        [HttpGet("inele")]
        [Authorize]
        public async Task<IActionResult> GetInelePrindere()
        {
            var inele = await _inelePrindereService.GetAllInelePrindere();
            
            return Ok(inele);
        }
        
        [HttpGet("inele/culori")]
        [Authorize]
        public async Task<IActionResult> GetColorsOfInele()
        {
            var ineleColors = await _inelePrindereService.GetAllIneleColors();

            return Ok(ineleColors);
        }
        
        [HttpGet("inel/{encodedIdInel}")]
        [Authorize]
        public async Task<IActionResult> GetInelById([FromRoute] string encodedIdInel)
        {
            if (_sqidsEncoder.Decode(encodedIdInel) is [var decodedId]
                && encodedIdInel == _sqidsEncoder.Encode(decodedId))
            {
                var inel = await _inelePrindereService.GetCurrentInelPage(decodedId);
        
                return Ok(inel);
            }
               
            return NotFound("Invalid material ID ");
            
        }
        
        [HttpPut("inel/{encodedIdInel}/image/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteImageForInel([FromRoute] string encodedIdInel)
        {

            if (_sqidsEncoder.Decode(encodedIdInel) is [var decodedId]
                && encodedIdInel == _sqidsEncoder.Encode(decodedId))
            {
                var inelImageDeletionResponse = await _inelePrindereService.DeleteInelPrindereImage(decodedId);

                return inelImageDeletionResponse switch
                {
                    1 => Ok("Deleted Succesfully"),
                    -3 => StatusCode(515, "Inel locked.Client is in buying session"),
                    _ => BadRequest("An error happenned when deleting the product")
                };
            }
            
            return BadRequest("Invalid id material");
            
        }
        
        [HttpPut("inel/{encodedIdInel}/update")]
        [Authorize]
        public async Task<IActionResult> UpdateInel([FromForm] IFormFileCollection image, [FromRoute] string encodedIdInel 
            , [FromForm] string modifiedInel)
        {
            Console.WriteLine($"{encodedIdInel}");
            if (_sqidsEncoder.Decode(encodedIdInel) is [var decodedId]
                && encodedIdInel == _sqidsEncoder.Encode(decodedId))
            {
                var modifiedInelToDto = JsonConvert.DeserializeObject<IneleDto>(modifiedInel);

                if (modifiedInelToDto is null)
                {
                    return NotFound("Inel not found");
                }
                
                Console.WriteLine($"{decodedId} , {modifiedInelToDto.CaleRelativa}");

                var response =
                    await _inelePrindereService.ModifyOrAddInelPrindere(modifiedInelToDto, image, decodedId, false);

                return response switch
                {
                    1 => Ok("Modified succesfully"),
                    -1 => BadRequest("NullReferenceException "),
                    -2 => StatusCode(515, "Duplicate image name in the bucket"),
                    -3 => NoContent(), // Inel locked. Client in buying session
                    _ => StatusCode(500, "General error occured")
                };
            }

            return NotFound("Invalid ID");
        }
        
        [HttpPost("inel/add")]
        [Authorize]
        public async Task<IActionResult> AddInel([FromForm] IFormFileCollection image, [FromForm] string newInel)
        {
            var newInelToDto = JsonConvert.DeserializeObject<IneleDto>(newInel);

            if (newInelToDto is null)
            {
                return NotFound("Inel converted to DTO is null");
            }

            var responseFromAddingInel = await _inelePrindereService.ModifyOrAddInelPrindere(newInelToDto, image, 0, true);
            
            return responseFromAddingInel switch
            {
                1 => Ok("Added succesfully"),
                -1 => BadRequest("NullReferenceException "),
                -2 => StatusCode(515, "Duplicate image name in the bucket"),
                _ => StatusCode(500, "General error occured")
            };
            
        }
        
        [HttpDelete("inel/delete/{encodedIdInel}")]
        [Authorize]
        public async Task<IActionResult> DeleteInelById([FromRoute] string encodedIdInel)
        {
            if (_sqidsEncoder.Decode(encodedIdInel) is [var decodedId]
                && encodedIdInel == _sqidsEncoder.Encode(decodedId))
            {
                var deleteInelResponse = await _inelePrindereService.DeleteInelPrindere(decodedId);

                return deleteInelResponse switch
                {
                    1 => Ok("Succesfully deleted inel"),
                    -3 => StatusCode(515, "Inel cannot be deleted. Client is in buying session"),
                    _ => BadRequest("An error happenned when deleting the material")
                };
            }

            return BadRequest("An error happenned when deleting the material");
        }

        [HttpPut("inele/deleteSelected")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedIneleById([FromBody] BulkOperationsDto deleteOperation)
        {
            var responseFromBulkDeletionOnInele =
                await _inelePrindereService.DeleteSelected(deleteOperation);

            return responseFromBulkDeletionOnInele switch
            {
                1 => Ok("Deleted succesfully the selected inele"),
                -3 => StatusCode(515, "Inel cannot be deleted. Client is in buying session"),
                _ => BadRequest("Error on deleting the selected inele")
            };
        }
        
        [HttpGet("tipuri_galerie")]
        [Authorize]
        public async Task<IActionResult> GetTipuriGalerie()
        {
            var tipuriGalerie = await _tipuriGalerieService.GetAllTipuriGalerie();
            
            return Ok(tipuriGalerie);
        }
        
        [HttpGet("tipuri_galerie/nume")]
        [Authorize]
        public async Task<IActionResult> GetNamesOfTipuriGalerie()
        {
            var tipuriGalerieNames = await _tipuriGalerieService.GetAllTipGalerieNames();

            return Ok(tipuriGalerieNames);
        }
        
        // nu mi vede endpointul
        
        [HttpGet("tip_galerie/{encodedIdTipGalerieDto}")]
        [Authorize]
        public async Task<IActionResult> GetTipGalerieById([FromRoute] string encodedIdTipGalerieDto)
        {
            Console.WriteLine($"encoded {encodedIdTipGalerieDto}");
            if (_sqidsEncoder.Decode(encodedIdTipGalerieDto) is [var decodedId]
                && encodedIdTipGalerieDto == _sqidsEncoder.Encode(decodedId))
            {
                Console.WriteLine($"decoded {decodedId}");
                var tipGalerie = await _tipuriGalerieService.GetCurrentTipGalerie(decodedId);
        
                return Ok(tipGalerie);
            }
               
            return NotFound("Invalid tip galerie ID ");
            
        }
        
        [HttpPut("tip_galerie/{encodedIdTipGalerie}/image/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteImageForTipGalerie([FromRoute] string encodedIdTipGalerie)
        {

            if (_sqidsEncoder.Decode(encodedIdTipGalerie) is [var decodedId]
                && encodedIdTipGalerie == _sqidsEncoder.Encode(decodedId))
            {
                var tipGalerieImageDeletionResponse = await _tipuriGalerieService.DeleteTipGalerieImage(decodedId);

                if (tipGalerieImageDeletionResponse == 1)
                {
                    return Ok("Deleted Succesfully");
                }
                return BadRequest("An error happenned when deleting the product");
            }
            
            return BadRequest("Invalid id tip galerie");
            
        }
        
        [HttpPut("tip_galerie/{encodedIdTipGalerie}/update")]
        [Authorize]
        public async Task<IActionResult> UpdateTipGalerie([FromForm] IFormFileCollection image, [FromRoute] string encodedIdTipGalerie 
            , [FromForm] string modifiedTipGalerie)
        {
            
         if (_sqidsEncoder.Decode(encodedIdTipGalerie) is [var decodedId]
                && encodedIdTipGalerie == _sqidsEncoder.Encode(decodedId))
            {
                var modifiedTipGalerieToDto = JsonConvert.DeserializeObject<TipuriGalerieDto>(modifiedTipGalerie);

                if (modifiedTipGalerieToDto is null)
                {
                    return NotFound("Inel not found");
                }
                
               
                var response =
                    await _tipuriGalerieService.ModifyOrAddTipGalerie(modifiedTipGalerieToDto, image, decodedId, false);

                return response switch
                {
                    1 => Ok("Modified succesfully"),
                    -1 => BadRequest("NullReferenceException "),
                    -2 => StatusCode(515, "Duplicate image name in the bucket"),
                    -3 => NoContent(), // Gallery locked. Cannot update. Client is in buying session
                    _ => StatusCode(500, "General error occured")
                };
            }

            return NotFound("Invalid ID");
        }
        
        [HttpPost("tip_galerie/add")]
        [Authorize]
        public async Task<IActionResult> AddTipGalerie([FromForm] IFormFileCollection image, [FromForm] string newTipGalerie)
        {
            var newTipGalerieToDto = JsonConvert.DeserializeObject<TipuriGalerieDto>(newTipGalerie);

            if (newTipGalerieToDto is null)
            {
                return NotFound("Tip galerie converted to DTO is null");
            }

            if (image.GetFile("image") is null)
            {
                Console.WriteLine("IMAGE IS NULL");
            }

            var responseFromAddingInel = await _tipuriGalerieService.ModifyOrAddTipGalerie(newTipGalerieToDto, image, 0, true);
            
            return responseFromAddingInel switch
            {
                1 => Ok("Added succesfully"),
                -1 => BadRequest("NullReferenceException "),
                -2 => StatusCode(515, "Duplicate image name in the bucket"),
                _ => StatusCode(500, "General error occured")
            };
            
        }
        
        [HttpDelete("tip_galerie/delete/{encodedIdTipGalerie}")]
        [Authorize]
        public async Task<IActionResult> DeleteTipGalerieById([FromRoute] string encodedIdTipGalerie)
        {
            if (_sqidsEncoder.Decode(encodedIdTipGalerie) is [var decodedId]
                && encodedIdTipGalerie == _sqidsEncoder.Encode(decodedId))
            {
                var deleteTipGalerieResponse = await _tipuriGalerieService.DeleteTipGalerie(decodedId);

                return deleteTipGalerieResponse switch
                {
                    1 => Ok("Succesfully deleted tip galerie"),
                    -3 => StatusCode(515, "Tip galerie folosit intr-o tranzactie"),
                    _ => BadRequest("An error happenned when deleting the tip galerie")
                };
            }

            return BadRequest("An error happenned when deleting thetip galerie");
        }

        [HttpPut("tipuri_galerie/deleteSelected")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedTipuriGalerieById([FromBody] BulkOperationsDto deleteOperation)
        {
            var responseFromBulkDeletionOnTipuriGalerie =
                await _tipuriGalerieService.DeleteSelected(deleteOperation);

            return responseFromBulkDeletionOnTipuriGalerie switch
            {
                1 => Ok("Deleted succesfully the selected tipuri galerie"),
                -3 => StatusCode(515, "Gallery locked. Client is buying ."),
                _ => BadRequest("Error on deleting the selected tipuri galerie")
            };
        }
        
        // ----------------------------
        // ----------------------------
        // ----------------------------
        // ----------------------------
        // ----------------------------

        
        
        [HttpGet("tipuri_linie")]
        [Authorize]
        public async Task<IActionResult> GetTipuriLinie()
        {
            var tipuriLinie = await _tipuriLinieService.GetAllTipuriLinie();
            
            return Ok(tipuriLinie);
        }
        
        [HttpGet("tipuri_linie/nume")]
        [Authorize]
        public async Task<IActionResult> GetNamesOfTipuriLinie()
        {
            var tipuriLinieNames = await _tipuriLinieService.GetAllTipuriLinieNames();

            return Ok(tipuriLinieNames);
        }
        
        // nu mi vede endpointul
        
        [HttpGet("tip_linie/{encodedIdTipLinie}")]
        [Authorize]
        public async Task<IActionResult> GetTipLinieById([FromRoute] string encodedIdTipLinie)
        {
          
            if (_sqidsEncoder.Decode(encodedIdTipLinie) is [var decodedId]
                && encodedIdTipLinie == _sqidsEncoder.Encode(decodedId))
            {
               
                var tipLinie = await _tipuriLinieService.GetCurrentTipLinie(decodedId);
        
                return Ok(tipLinie);
            }
               
            return NotFound("Invalid tip linie ID ");
            
        }
        
        [HttpPut("tip_linie/{encodedIdTipLinie}/image/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteImageForTipLinie([FromRoute] string encodedIdTipLinie)
        {

            if (_sqidsEncoder.Decode(encodedIdTipLinie) is [var decodedId]
                && encodedIdTipLinie == _sqidsEncoder.Encode(decodedId))
            {
                var tipGalerieImageDeletionResponse = await _tipuriLinieService.DeleteTipLinieImage(decodedId);

                return tipGalerieImageDeletionResponse switch
                {
                    1 => Ok("Deleted Succesfully"),
                    -3 => StatusCode(515, "Line type locked. Wait for client to finish buying session"),
                    _ => BadRequest("An error happenned when deleting the product")
                };
            }
            
            return BadRequest("Invalid id tip linie");
            
        }
        
        [HttpPut("tip_linie/{encodedIdTipLinie}/update")]
        [Authorize]
        public async Task<IActionResult> UpdateTipLinie([FromForm] IFormFileCollection image, [FromRoute] string encodedIdTipLinie 
            , [FromForm] string modifiedTipLinie)
        {
            
         if (_sqidsEncoder.Decode(encodedIdTipLinie) is [var decodedId]
                && encodedIdTipLinie == _sqidsEncoder.Encode(decodedId))
            {
                var modifiedTipLinieToDto = JsonConvert.DeserializeObject<TipuriLinieDto>(modifiedTipLinie);

                if (modifiedTipLinieToDto is null)
                {
                    return NotFound("Tip linie not found");
                }
                
               
                var response =
                    await _tipuriLinieService.ModifyOrAddTipLinie(modifiedTipLinieToDto, image, decodedId, false);

                return response switch
                {
                    1 => Ok("Modified succesfully"),
                    -1 => BadRequest("NullReferenceException "),
                    -2 => StatusCode(515, "Duplicate image name in the bucket"),
                    -3 => NoContent(), // 204 , Locked , cannot delete . Client is in buying session
                    _ => StatusCode(500, "General error occured")
                };
            }

            return NotFound("Invalid ID");
        }
        
        [HttpPost("tip_linie/add")]
        [Authorize]
        public async Task<IActionResult> AddTipLinie([FromForm] IFormFileCollection image, [FromForm] string newTipLinie)
        {
            var newTipLinieDto = JsonConvert.DeserializeObject<TipuriLinieDto>(newTipLinie);

            if (newTipLinieDto is null)
            {
                return NotFound("Tip linie converted to DTO is null");
            }

            if (image.GetFile("image") is null)
            {
                Console.WriteLine("IMAGE IS NULL");
            }

            var responseFromAddingTipLinie = await _tipuriLinieService.ModifyOrAddTipLinie(newTipLinieDto, image, 0, true);
            
            return responseFromAddingTipLinie switch
            {
                1 => Ok("Added succesfully"),
                -1 => BadRequest("NullReferenceException "),
                -2 => StatusCode(515, "Duplicate image name in the bucket"),
                _ => StatusCode(500, "General error occured")
            };
            
        }
        
        [HttpDelete("tip_linie/delete/{encodedIdTipLinie}")]
        [Authorize]
        public async Task<IActionResult> DeleteTipLinieById([FromRoute] string encodedIdTipLinie)
        {
            if (_sqidsEncoder.Decode(encodedIdTipLinie) is [var decodedId]
                && encodedIdTipLinie == _sqidsEncoder.Encode(decodedId))
            {
                var deleteTipLinieResponse = await _tipuriLinieService.DeleteTipLinie(decodedId);

                return deleteTipLinieResponse switch
                {
                    1 => Ok("Succesfully deleted tip linie"),
                    -3 => StatusCode(515, "Line type is locked.Wait for client to finish buying session"),
                    _ => BadRequest("An error happenned when deleting the tip galerie")
                };
            }

            return BadRequest("An error happenned when deleting the tip linie");
        }

        [HttpPut("tipuri_linie/deleteSelected")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedTipuriLinieById([FromBody] BulkOperationsDto deleteOperation)
        {
            var responseFromBulkDeletionOnTipuriLinie =
                await _tipuriLinieService.DeleteSelected(deleteOperation);

            return responseFromBulkDeletionOnTipuriLinie switch
            {
                1 => Ok("Deleted succesfully the selected tipuri linie"),
                -3 => StatusCode(515, "Line type locked. Cannot delete"),
                _ => BadRequest("Error on deleting the selected tipuri linie")
            };
        }
        
        // VOUCHERS
        /*
         * ----------
         * ----------
         */
        
        [HttpGet("vouchere")]
        [Authorize]
        public async Task<IActionResult> GetVouchere()
        {
            var vouchere = await _voucherService.GetAllVouchers();
            
            return Ok(vouchere);
        }
        
        [HttpGet("vouchere/coduri")]
        [Authorize]
        public async Task<IActionResult> GetVouchereCodes()
        {
            var voucherCodes = await _voucherService.GetAllVoucherCodes();

            return Ok(voucherCodes);
        }
        
        [HttpGet("voucher/{encodedIdVoucher}")]
        [Authorize]
        public async Task<IActionResult> GetVoucherById([FromRoute] string encodedIdVoucher)
        {
          
            if (_sqidsEncoder.Decode(encodedIdVoucher) is [var decodedId]
                && encodedIdVoucher == _sqidsEncoder.Encode(decodedId))
            {
               
                var voucher = await _voucherService.GetCurrentVoucher(decodedId);
        
                return Ok(voucher);
            }
               
            return NotFound("Invalid voucher ID ");
            
        }
        
        [HttpPut("voucher/{encodedIdVoucher}/update")]
        [Authorize]
        public async Task<IActionResult> UpdateVoucher([FromRoute] string encodedIdVoucher 
            , [FromForm] string modifiedVoucher)
        {
            
         if (_sqidsEncoder.Decode(encodedIdVoucher) is [var decodedId]
                && encodedIdVoucher == _sqidsEncoder.Encode(decodedId))
            {
                var modifiedVoucherToDto = JsonConvert.DeserializeObject<VouchereDto>(modifiedVoucher);

                if (modifiedVoucherToDto is null)
                {
                    return NotFound("voucher not found");
                }
                
               
                var response =
                    await _voucherService.ModifyOrAddVoucher(modifiedVoucherToDto, decodedId, false);

                return response switch
                {
                    1 => Ok("Modified succesfully"),
                    -1 => BadRequest("NullReferenceException "),
                    _ => StatusCode(500, "General error occured")
                };
            }

            return NotFound("Invalid ID");
        }
        
        [HttpPost("voucher/add")]
        [Authorize]
        public async Task<IActionResult> AddVoucher([FromForm] string newVoucher)
        {
            var newVoucherToDto = JsonConvert.DeserializeObject<VouchereDto>(newVoucher);

            if (newVoucherToDto is null)
            {
                return NotFound("Voucher converted to DTO is null");
            }
            
            var responseFromAddingTipLinie = await _voucherService.ModifyOrAddVoucher(newVoucherToDto,  0, true);
            
            return responseFromAddingTipLinie switch
            {
                1 => Ok("Added succesfully"),
                -1 => BadRequest("NullReferenceException "),
                _ => StatusCode(500, "General error occured")
            };
            
        }
        
        [HttpDelete("voucher/delete/{encodedIdVoucher}")]
        [Authorize]
        public async Task<IActionResult> DeleteVoucherById([FromRoute] string encodedIdVoucher)
        {
            if (_sqidsEncoder.Decode(encodedIdVoucher) is [var decodedId]
                && encodedIdVoucher == _sqidsEncoder.Encode(decodedId))
            {
                var deleteVoucherReponse = await _voucherService.DeleteVoucher(decodedId);

                if (deleteVoucherReponse == 1)
                {
                    return Ok("Succesfully deleted voucher");
                }
                return BadRequest("An error happenned when deleting the voucher");
            }

            return BadRequest("An error happenned when deleting the voucher");
        }

        [HttpPut("vouchere/deleteSelected")]
        [Authorize]
        public async Task<IActionResult> DeleteSelectedVoucherById([FromBody] BulkOperationsDto deleteOperation)
        {
            var responseFromBulkDeletionOnTipuriLinie =
                await _voucherService.DeleteSelected(deleteOperation);

            if (responseFromBulkDeletionOnTipuriLinie == 1)
            {
                return Ok("Deleted succesfully the selected vouchers");
            }

            return BadRequest("Error on deleting the selected vouchers");
        }
        
        
        // END VOUCHERS
        /*
         * ---------------------
         * ---------------------
         */
        

        [HttpGet("customers")]
        [Authorize]
        public async Task<IActionResult> GetClientsForAdminDisplay()
        {
            var clients = await _userService.GetAccountForAdmin();
            return Ok(clients);
        }

        [HttpGet("customer/{encodedIdAccount}")]
        [Authorize]
        public async Task<IActionResult> GetClientDataAccount([FromRoute] string encodedIdAccount)
        {
            if (_sqidsEncoder.Decode(encodedIdAccount) is [var decodedId]
                && encodedIdAccount == _sqidsEncoder.Encode(decodedId))
            {
                var dtoToReturn = await _userService.GetAccountData(decodedId);

                if (dtoToReturn is null)
                {
                    return NotFound("Account data not found , an error took place");
                }

                return Ok(dtoToReturn);
            }

            return NotFound("Invalid decoded id");
        }

        [HttpGet("downloadExcel")]
        [Authorize]
        public async Task<IActionResult> GetExcelFile()
        {
            var fileStream = await _bucketAcces.DownloadFile("example/example.xlsx");

            if (fileStream == null)
            {
                return NotFound("Error downloading file");
            }
           
            return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            
        }

        [HttpPut("customer/data/update")]
        [Authorize]
        public async Task<IActionResult> UpdatePersonalData([FromForm] string updatedClientDataDto)
        {
            var clientDataToDto = JsonConvert.DeserializeObject<ConturiDtoForModification>(updatedClientDataDto);
            if (clientDataToDto is null)
            {
                return BadRequest("Could not deserialize the object from the form");
            }

            var resposeFromUpdatingData = await _adminService.SaveAdminPersonalDataModification(clientDataToDto);

            return resposeFromUpdatingData switch
            {
                1 => Ok("Data has been updated! No email has been changed"),
                2 => Accepted("Data has been changed , but email as well! Proceed."),
                -2 => StatusCode(515, "Invalid email exception!"),
                -1 => NotFound("General error occured. Transaction rolled back!"),
                _ => StatusCode(500, "General error occured, 500 response!"),
            };
        }

        [HttpPut("customer/update/address/state/{alias}/{addressState:bool}/{tipAdresa}/{encodedIdAccountDto}")]
        [Authorize]
        public async Task<IActionResult> UpdateAddressState([FromRoute] string alias, [FromRoute] bool addressState,
            [FromRoute] string encodedIdAccountDto , [FromRoute] TipAdrese tipAdresa)
        {
            if (_sqidsEncoder.Decode(encodedIdAccountDto) is [var decodedId]
                && encodedIdAccountDto == _sqidsEncoder.Encode(decodedId))
            {
                var responseFromModifyingAddressState = await 
                    _adminService.ModifyAddressState(alias, addressState, tipAdresa,decodedId);

                return responseFromModifyingAddressState switch
                {
                    1 => Ok("Succesfully modified the address state"),
                    _ => NotFound("General error occured")
                };
            }

            return BadRequest("Could not decode id!");
        }

        [HttpPut("customer/update/addresses")]
        [Authorize]
        public async Task<IActionResult> ModifyAddresses([FromForm] string updatedAddresses)
        {
            var dataToDto = JsonConvert.DeserializeObject<ConturiDtoForModification>(updatedAddresses);
            
            
            var responseFromModifyingAddresses = await 
                _adminService.ModifyAddresses(dataToDto!);

            return responseFromModifyingAddresses switch
            {
                1 => Ok("Succesfully modified the addresses"),
                _ => NotFound("General error occured")
            };
        }

        [HttpGet("mainOrders")]
        [Authorize]
        public async Task<IActionResult> GetMainOrders()
        {
            var orders = await _adminService.GetAllOrders();

            return Ok(orders);
        }


        [HttpGet("manopere")]
        [Authorize]
        public async Task<IActionResult> GetManopere()
        {
            var manopere = await _manopereService.GetManopere();
            return Ok(manopere);
        }
        
        
        [HttpGet("manopera/{encodedIdManopera}")]
        [Authorize]
        public async Task<IActionResult> GetManopere([FromRoute] string encodedIdManopera)
        {
            if (_sqidsEncoder.Decode(encodedIdManopera) is [var decodedId]
                && encodedIdManopera == _sqidsEncoder.Encode(decodedId))
            {
                var manoperaPage = await _manopereService.GetManoperaForModification(decodedId , TipManopere.Standard);
                if (manoperaPage == null)
                {
                    return NotFound("Error occured!Lining type or Ring type or Galery type not found");
                }
                
                return Ok(manoperaPage);
            }

            return BadRequest("Invalid id after decoding");
        }

        [HttpGet("manopere/options")]
        [Authorize]
        public async Task<IActionResult> GetManopereOptions()
        {
            var options = await _manopereService.GetAvailableOptions();
            return Ok(options);
        }
        
        [HttpPost("manopera/{encodedIdManopera}/{isUpdating:bool}")]
        [Authorize]
        public async Task<IActionResult> GetManopere([FromRoute] string encodedIdManopera, [FromRoute] bool isUpdating,
            [FromForm] string manoperaUpdated)
        {
            
            int? decodedId = null;
            if (encodedIdManopera != "0")
            {
                if (_sqidsEncoder.Decode(encodedIdManopera) is [var id]
                    && encodedIdManopera == _sqidsEncoder.Encode(id))
                {
                    decodedId = id;
                }
                else
                {
                    return BadRequest("Invalid id after decoding");
                }
            }

          
            var convertToDto = JsonConvert.DeserializeObject<ManoperaPageModification>(manoperaUpdated);

            if (convertToDto == null)
            {
                return StatusCode(505, "Error occurred when decoding the DTO");
            }

            
            var responseFromAddOrUpdateManopera =
                await _manopereService.ModifyOrAddManopera(convertToDto, isUpdating, decodedId);
            Console.WriteLine("Raspuns {0}" , responseFromAddOrUpdateManopera);
            return responseFromAddOrUpdateManopera switch
            {
                1 => Ok("Updated succesfully"), // modified succesfully
                2 => NoContent(), // created succesfully
                // General error thrown, not treated.
                -1 => BadRequest("General error thrown when executing transaction (update or add)"), 
                // check ManopereService in ModifyOrAddManopera(exception thrown : ArgumentNullException || InvalidOperationException)
                -2 => NotFound("Either a ring type or a line type or a galery type does not exist"),
                -3 => StatusCode(515 , "Manopera este blocata. Un client cumpara un produs ce foloseste aceasta manopera"),
                // server error     
                _ => StatusCode(500, "General error occured. Check logs")
            };
        }

        [HttpPost("modifyGeneralSettings")]
        [Authorize]
        public async Task<IActionResult> ModifyGeneralSettings([FromForm] string dict)
        {
            var generalSettingsDict = JsonConvert.DeserializeObject<IDictionary<string, string>>(dict);

            if (generalSettingsDict == null) return NotFound("Contactati administratorului");
            
            var response = await _generalSettingsService.SaveGeneralSettings(generalSettingsDict);

            return response switch
            {
                -2 => BadRequest("Eroare generala. Contactati administratorului"),
                1 => Ok("Modificat cu succes"),
                _ => StatusCode(500 , "Server error"),
            };

        }

        [HttpGet("generalSettings")]
        [Authorize]
        public async Task<IActionResult> GetGeneralSettings()
        {
            var response = await _generalSettingsService.GetGeneralSettingsData();
            return Ok(response);
        }

       
    }
    
    
    
    
    
}