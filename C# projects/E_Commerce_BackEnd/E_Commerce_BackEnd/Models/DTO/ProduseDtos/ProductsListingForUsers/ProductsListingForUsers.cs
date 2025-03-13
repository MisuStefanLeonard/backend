using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers;

public class ProductsListingForUsers
{
    public string CodProdusDto { get; init; } = null!;
    public string NumeProdusDto { get; init; } = null!;
    public Nume NumeProdusJsonDto { get; init; } = null!;
    public string TipulProdusuluiDto { get; init; } = null!;
    public TipProdus TipulProdusuluiJsonDto { get; init; } = null!;
    public decimal PretBazaDto { get; init; } 
    public decimal PretBazaRedusDto { get; init; } 
    public IList<DimensiuniDto>? DimensiuniProduseDto { get; init; } = [];
    public IList<ColorsWithImages> CuloriProdusDto { get; init; } = [];
    public IList<ColorsWithImages> CuloriProdusJsonDto { get; init; } = [];

    public ReviewsInfoForQuickDisplay ReviewsInfoGeneral { get; init; } = null!;
    public int TotalProducts { get; init; }
}