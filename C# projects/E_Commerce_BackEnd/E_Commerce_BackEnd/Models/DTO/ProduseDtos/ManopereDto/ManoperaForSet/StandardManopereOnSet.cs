using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;

public class StandardManopereOnSet
{
    public int? IdManopera { get; init; }
    public string NumeManopera { get; init; } = null!;
    public Nume? NumeManoperaJson { get; init; } = null!;
    public decimal MetruTotalFolosit { get; init; }
    public string? InaltimeMaxima { get; init; }
    public TipIneleDto? TipInel { get; init; }
    public TipRejansaDto TipGalerie { get; init; } = null!;
    public TipLinieDto TipLinie { get; init; } = null!;
}