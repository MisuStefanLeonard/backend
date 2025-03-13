using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;

public class ProductsInSet
{
    public string NumeProdusDto { get; set; } = null!;
    public Nume NumeProdusJsonDto { get; set; } = null!;

    public IList<ColorsWithImages> CuloriProdusDto { get; init; } = [];
}