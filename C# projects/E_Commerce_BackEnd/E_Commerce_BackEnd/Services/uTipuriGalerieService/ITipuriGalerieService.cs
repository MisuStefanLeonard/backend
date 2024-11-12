using E_Commerce_BackEnd.Models.DTO.ProduseDtos.BulkOperationsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;

namespace E_Commerce_BackEnd.Services.uTipuriGalerieService;

public interface ITipuriGalerieService
{
    public Task<IList<TipuriGalerieDisplayDto>?> GetAllTipuriGalerie();
    public Task<IList<string>?> GetAllTipGalerieNames();
    public Task<TipuriGalerieDto?> GetCurrentTipGalerie(int idTipGalerie);
    public Task<int?> DeleteTipGalerie(int idTipGalerie); 
    public Task<int> DeleteTipGalerieImage(int idTipGalerie);
    public Task<int> DeleteSelected(BulkOperationsDto deleteSelected);
    public Task<int> ModifyOrAddTipGalerie(TipuriGalerieDto tipuriGalerieDto, IFormFileCollection? image, int idTipGalerie , bool isAdded);
}