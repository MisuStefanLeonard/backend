using E_Commerce_BackEnd.Models.ConfigurationModels;
using E_Commerce_BackEnd.Models.DTO.AdminRelatedDtos.PopUps;

namespace E_Commerce_BackEnd.Services.uPopUpService;

public interface IPopUpService
{
    public Task<IList<PopUpListDto>?> GetAllPopUps();
    public Task<KeyValuePair<int,int>> CreatePopUp(PopUpDto popUpDto);
    public Task<int> UpdatePopUp(PopUpDto popUpDto);
    public Task<int> DeletePopUp(int id);
    public Task<int> DeactivatePopUp(int id);
}