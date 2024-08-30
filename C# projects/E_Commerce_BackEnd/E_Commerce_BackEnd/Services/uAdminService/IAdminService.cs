using E_Commerce_BackEnd.Models.DTO.ProduseDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels;

namespace E_Commerce_BackEnd.Services.uAdminService;

public interface IAdminService
{
    public Task<int> AdminLogIn(string key);
    public Task<IList<ProduseDtoForAdminListing>?> GetProductsForDtoAdminListing();

    public Task<ProduseDtoForAdminModification?> GetProductForAdminPage(string codProdus);
}