using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ShoppingCartDtos;

public class CartItems
{
    public int IdProdus { get; init; }
    public int? IdSet { get; init; }
    public string? NumeSet { get; init; }
    // public Nume? NumeSetJson { get; init; } = null;
    public string CodProdus { get; init; } = null!;
    public string NumeProdus { get; init; } = null!;
    // public Nume NumeProdusjson { get; init; } = null!;
    public string TipProdus { get; init; } = null!;
    public TipProdus TipProdusJson { get; init; } = null!;
    public CuloriDto CuloareSelectata { get; init; } = null!;
    public DimensiuniDto? DimensiuneSelectata { get; init; }
    public StandardManopereOnSet? SelectedManopera { get; init; }
    public string LungimeCeruta { get; init; } = null!;
    public string? InaltimeCeruta { get; init; }
    public decimal PretCurent { get; init; } 
    public decimal PretReal { get; init; }
    public int Cantitate { get; init; }
    public string IdentificatorSet { get; init; } = null!;
    
}