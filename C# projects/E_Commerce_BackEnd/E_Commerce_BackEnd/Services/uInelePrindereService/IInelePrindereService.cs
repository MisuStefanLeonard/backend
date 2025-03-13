using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;


namespace E_Commerce_BackEnd.Services.uInelePrindereService;

public interface IInelePrindereService
{
    public Task<IList<IneleDisplayDto>?> GetAllInelePrindere();
    public Task<IList<Culoare>?> GetAllIneleColors();
    public Task<IneleDto?> GetCurrentInelPage(int idInelPrindere);
    public Task<int?> DeleteInelPrindere(int idInelPrindere); 
    public Task<int> DeleteInelPrindereImage(int idInelPrindere);
    public Task<int> DeleteSelected(BulkOperationsDto deleteSelected);
    public Task<int> ModifyOrAddInelPrindere(IneleDto ineleDto, IFormFileCollection? image, int idInelPrindere , bool isAdded);
}