using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;

public class SetsListingForUsers
{
    public string EncodedIdSet { get; init; } = null!;
    public string NumeSetDto { get; init; } = null!;
    public decimal PretSetDto { get; init; }
    public decimal PretRedusSetDto { get; init; }
    public ReviewsInfoForQuickDisplay ReviewsInfoGeneral { get; init; } = null!;
    public IList<ProductsInSet> SetProductsDto { get; init; } = new List<ProductsInSet>();
}