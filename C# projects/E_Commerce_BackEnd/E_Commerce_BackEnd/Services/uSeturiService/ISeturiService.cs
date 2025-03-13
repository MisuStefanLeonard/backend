using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Services.uSeturiService;

public interface ISeturiService
{
    #region AdminCRUD
    public Task<IList<SeturiDisplayDto>?> GetProductSets();
    public Task<SetModificationDto?> GetSetPage(int idSet);
    public Task<IList<SelectProducts>> GetProductCodes();
    public Task<IList<Nume>> GetSetNames();
    public Task<int> DeleteSet(int idSet);
    public Task<int> DeleteBulkSets(BulkOperationsDto sets);
    public Task<int> ActivateSet(int idSet, bool activationState);
    public Task<int> ActivateBulkSets(BulkOperationsDto sets);
    public Task<int> DeleteProductFromSet(int idSet,int idProdus );
    public Task<int> AddOrUpdateSet(SetModificationDto modifiedSet,int idSet,bool isAdding);
    public Task<ProductForSetDto?> GetProductDataForSetAdd(string codProdus);

    #endregion

    #region User

    public Task<FinalSetDisplayForUsers> GetSetsForUsers(int? pageNumber, 
        List<string>? productTypes,
        List<decimal>? productPrices ,
        string? productName
        ,string currency = "RON");


    public Task<KeyValuePair<int,SetPage?>> GetSetForUser(int setId,string setName,string currency = "RON");

    #endregion

}