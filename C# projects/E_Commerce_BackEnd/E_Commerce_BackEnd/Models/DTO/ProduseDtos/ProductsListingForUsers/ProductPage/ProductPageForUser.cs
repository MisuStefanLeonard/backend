using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;

public class ProductPageForUser
{
    public int IdProdus { get; init; }
    public string? CodProdusDto { get; init; }
    public string? DescriereDto { get; init; }
    public Descriere? DesriereJsonDto { get; init; }
    public string? NumeProdusDto { get; init; }
    public Nume NumeProdusJsonDto { get; init; } = null!;
    public string? CompozitieDto { get; init; }
    public Compozitie? CompozitieJsonDto { get; init; }
    public byte TvaDto { get; init; }
    public string? IngrijireDto { get; init; }
    public Ingrijire? IngrijireJsonDto { get; init; } 
    public bool? FataReversibilaDto { get; init; }
    public string TipulProdusuluiDto { get; init; } = null!;
    public TipProdus TipulProdusuluiJsonDto { get; init; } = null!;
    public string? NumeProducatorDto { get; init; }
    public decimal PretBazaDto { get; init; } 
    public decimal PretBazaRedusDto { get; init; }
    public decimal InaltimeMaximaDto { get; init; }
    
    // in case of perdea/draperie
    public IList<TipIneleDto> TipuriInele { get; init; } = new List<TipIneleDto>();
    public IList<TipLinieDto> TipuriLinie { get; init; } = new List<TipLinieDto>();
    public IList<TipRejansaDto> TipuriRejansa { get; init; } = new List<TipRejansaDto>();

    public IList<DimensiuniDto> DimensiuniProdus { get; init; } = new List<DimensiuniDto>();
    public IList<CuloriDto> CuloriProdus { get; init; } = new List<CuloriDto>();
    public IList<CuloriDto> CuloriProdusJson { get; init; } = new List<CuloriDto>();

    public IList<ReviewsDto> ReviewsProdus { get; init; } = new List<ReviewsDto>();

    public IList<string> CategoriiProdus { get; init; } = new List<string>();
    public IList<Categorie> CategoriiProdusJson { get; init; } = new List<Categorie>();

    public ReviewsInfo? ReviewsGeneral { get; init; }
}