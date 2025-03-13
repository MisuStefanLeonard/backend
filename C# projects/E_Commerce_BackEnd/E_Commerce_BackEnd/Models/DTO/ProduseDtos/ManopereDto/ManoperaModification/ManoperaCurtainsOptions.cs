using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaModification;

public class ManoperaCurtainsOptions
{
    public IList<TipRejansaDto> RejanseDisponibile { get; init; } = new List<TipRejansaDto>();
    public IList<TipIneleDto> IneleDisponibile { get; init; } = new List<TipIneleDto>();
    public IList<TipLinieDto> CusaturiLiniiDisponibile { get; init; } = new List<TipLinieDto>();
    // public IList<string?> NumeManopereFolosite { get; init; } = new List<string>();
    public IList<Nume?> NumeManopereFolosite { get; init; } = new List<Nume?>();

}