using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;

public class ManoperaCurtainsOptions
{
    public IList<TipRejansaDto> RejanseDisponibile { get; init; } = new List<TipRejansaDto>();
    public IList<TipIneleDto> IneleDisponibile { get; init; } = new List<TipIneleDto>();
    public IList<TipLinieDto> CusaturiLiniiDisponibile { get; init; } = new List<TipLinieDto>();
    public IList<string?> NumeManopereFolosite { get; init; } = new List<string>();
}