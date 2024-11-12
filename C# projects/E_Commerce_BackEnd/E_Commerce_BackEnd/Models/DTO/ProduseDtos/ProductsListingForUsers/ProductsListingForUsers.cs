using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers;

public class ProductsListingForUsers
{
    public string CodProdusDto { get; init; } = null!;
    public string NumeProdusDto { get; init; } = null!;
    public string TipulProdusuluiDto { get; init; } = null!;
    public decimal PretBazaDto { get; init; } 
    public decimal PretBazaRedusDto { get; init; } 
    public IList<DimensiuniDto>? DimensiuniProduseDto { get; init; } = [];
    public IList<ColorsWithImages> CuloriProdusDto { get; init; } = [];
    
    public int TotalProducts { get; init; }
}