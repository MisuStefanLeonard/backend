using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;

public class ProductsInSet
{
    public string NumeProdusDto { get; set; } = null!;
    public IList<ColorsWithImages> CuloriProdusDto { get; init; } = [];
}