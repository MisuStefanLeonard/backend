using System.Collections.Immutable;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Services.uProductsService;

public interface IProductService
{
    #region CRUD
    public Task<int> DeleteProduct(string codProdus);
    public Task<int[]> UpdateProduct(ProduseDtoForAdminModification modifiedProduct, IFormFileCollection images);
    public Task<int> AddOrEditProductFromExcel(ProduseDto produseDto, IList<int> idDimensiuni,
        string[] filePath , IList<int> idTipProduse,  IList<int> culoriId , 
        string[] preturiPerDimensiuni , int idProducator , string tipProdus,
            string folderName);
    
    
    #endregion

    #region ProductCharacteristicsCRUD
    public Task<int> DeleteTypeOnProduct(string codProdus,string categorieProdus);
    public Task<int> DeleteDimensionOnProduct(string codProdus, string lungime,string latime,string pret,string recomandarePat);
    public Task<int> DeleteColorOnProduct(string codProdus, string numeCuloare,string codCuloare);
    public Task<int> DeleteImageOnProduct(string codProdus,string numeCuloare,string codCuloare ,string caleImagine,string fisierInBucket);
    public Task<int> ToggleActivationStateInShop(string productCode, bool activation);
    public Task<int> DeleteSelectedProducts(BulkOperationsDto bulkOperationsDto);
    public Task<int> ActivateSelectedProducts(BulkOperationsDto bulkOperationsDto);
    
    #endregion
    
    #region GetProductOptionsData
    public Task<ProductOptionsForComboBox?> GetProductTypes();
    public Task<IList<ProductInfo>?> GetProductCodesAndNames();
    public Task<ProductTypesAndSubCategories> GetProductTypesAndSubCategories();
    #endregion

    #region ProductsForUsers

    public Task<IList<ProductsListingForUsers>> GetProductsForUsers(int? pageNumber, List<string>? productTypes ,List<string>? productColors,
        List<string>? productDimensions , List<decimal>? productPrices , bool? reverseFace,string currency = "RON");
    public Task<ProductsFilterOptions> FilterOptions(string currency = "RON");
    public Task<KeyValuePair<int , ProductPageForUser?>> GetProductPage(string codProdus , string tipProdus,string currency = "RON");
    public Task<IList<MostViewedProduct>> GetMostViewedProducts(string currency = "RON");
    public Task<IList<MostViewedProduct>> GetProductsThatAreNew(string currency = "RON");
    public Task<IList<MostViewedProduct>>  GetProductsThatAreLimitedEdition(string currency = "RON");
   

    #endregion



}