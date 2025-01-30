using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;

public class ProductPageForUser
{
    public int IdProdus { get; init; }
    public string? CodProdusDto { get; init; }
    public string? DescriereDto { get; init; }
    public string? NumeProdusDto { get; init; }
    public string? CompozitieDto { get; init; }
    public byte TvaDto { get; init; }
    public string? IngrijireDto { get; init; }
    public bool? FataReversibilaDto { get; init; }
    public string TipulProdusuluiDto { get; init; } = null!;
    public string? NumeProducatorDto { get; init; }
    public decimal PretBazaDto { get; init; } 
    public decimal PretBazaRedusDto { get; init; }
    
    // in case of perdea/draperie
    public IList<TipIneleDto> TipuriInele { get; init; } = new List<TipIneleDto>();
    public IList<TipLinieDto> TipuriLinie { get; init; } = new List<TipLinieDto>();
    public IList<TipRejansaDto> TipuriRejansa { get; init; } = new List<TipRejansaDto>();

    public IList<DimensiuniDto> DimensiuniProdus { get; init; } = new List<DimensiuniDto>();
    public IList<CuloriDto> CuloriProdus { get; init; } = new List<CuloriDto>();
    public IList<ReviewsDto> ReviewsProdus { get; init; } = new List<ReviewsDto>();

    public IList<string> CategoriiProdus { get; init; } = new List<string>();
    public ReviewsInfo? ReviewsGeneral { get; init; }
}