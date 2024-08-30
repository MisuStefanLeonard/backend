using E_Commerce_BackEnd.Models.DTO;
using E_Commerce_BackEnd.Models.OrderRelatedModels;
using E_Commerce_BackEnd.Models.UserRelatedModels;

namespace E_Commerce_BackEnd.Services.uAdressService;

public interface IAdressService
{
    public Task<IList<AdreseDto>?> GetAllUsersAdresses(int userId);
    public Task<int> SaveAddress(AdreseDto adressDto , int userId);

    public Task<int> DeleteAddress(int userId,string alias);
}