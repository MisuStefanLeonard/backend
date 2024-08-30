using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Services.uProductsService;

public interface IProductService
{
    #region CRUD
    public Task<int> DeleteProduct(string codProdus);
    public Task<int[]> UpdateProduct(ProduseDtoForAdminModification modifiedProduct);
    public Task<int> AddOrEditProductFromExcel(ProduseDto produseDto, IList<int> idDimensiuni,
        string[] filePath , IList<int> idTipProduse,  IList<int> culoriId , 
        string[] preturiPerDimensiuni , int idProducator , string tipProdus,
            string folderName);
    
    
    #endregion

    #region ProductCharacteristicsCRUD

    public Task<int> DeleteTypeOnProduct(string codProdus,string tipProdus,string categorieProdus);
    public Task<int> DeleteDimensionOnProduct(string codProdus, string lungime,string latime,string pret,string recomandarePat);
    public Task<int> DeleteColorOnProduct(string codProdus, string numeCuloare,string codCuloare);
    public Task<int> DeleteImageOnProduct(string codProdus,string numeCuloare,string codCuloare ,string caleImagine,string fisierInBucket);

    public Task<int> DeleteVoucherOnProduct(string codProdus, string codVoucher);
    public Task<string> GeneratePresignedUrl(string caleImagini, string fisierInBucket); 

    #endregion

    #region GetProductOptionsData

    public Task<ProductOptionsForComboBox?> GetProductTypes();

    #endregion



}