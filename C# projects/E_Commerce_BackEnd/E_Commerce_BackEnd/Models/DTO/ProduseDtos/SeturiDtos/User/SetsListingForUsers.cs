using Sqids;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;

public class SetsListingForUsers
{
    public string EncodedIdSet { get; init; } = null!;
    public string NumeSetDto { get; init; } = null!;
    public decimal PretSetDto { get; init; }
    public decimal PretRedusSetDto { get; init; }
    public IList<ProductsInSet> SetProductsDto { get; init; } = new List<ProductsInSet>();
}