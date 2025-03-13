using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Services.uTipuriLinieService;

public interface ITipuriLinieService
{
    public Task<IList<TipuriLinieDisplayDto>?> GetAllTipuriLinie();
    public Task<IList<Nume>?> GetAllTipuriLinieNames();
    public Task<TipuriLinieDto?> GetCurrentTipLinie(int idTipLinie);
    public Task<int?> DeleteTipLinie(int idTipLinie); 
    public Task<int> DeleteTipLinieImage(int idTipLinie);
    public Task<int> DeleteSelected(BulkOperationsDto deleteSelected);
    public Task<int> ModifyOrAddTipLinie(TipuriLinieDto tipuriLinieDto, IFormFileCollection? image, int idTipLinie , bool isAdded);

}