using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;
using E_Commerce_BackEnd.Models.Enums;

namespace E_Commerce_BackEnd.Services.uManopereService;

public interface IManopereService
{
    public Task<IList<ManopereListingDto>> GetManopere();
    public Task<ManoperaPageModification?> GetManoperaForModification(int manoperaId , TipManopere tipManopera);
    public Task<ManoperaCurtainsOptions> GetAvailableOptions();
    public Task<int> ModifyOrAddManopera(ManoperaPageModification manopera, bool isUpdating , int? idManopera);
}