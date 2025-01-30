using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;

public class MostViewedProduct
{
    public string CodProdusDto { get; init; } = null!;
    public string NumeProdusDto { get; init; } = null!;
    public string TipulProdusuluiDto { get; init; } = null!;
    public decimal PretBazaDto { get; init; } 
    public decimal PretBazaRedusDto { get; init; } 
    public IList<ColorsWithImages> CuloriProdusDto { get; init; } = [];
}