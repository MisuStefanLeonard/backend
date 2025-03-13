using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User.SetPage;

public class SetPage
{
    public string NumeSetDto { get; init; } = null!;
    public Nume NumeSetJsonDto { get; init; } = null!;
    public decimal PretSetDto { get; init; }
    public decimal PretRedusSetDto { get; init; }
    public string DescriereSetDto { get; init; } = null!;
    public Descriere DescriereJsonDto { get; init; } = null!;

    public IList<ProductOnSet> ProdusePeSet { get; init; } = new List<ProductOnSet>();
    public IList<ReviewsDto> ReviewsSet { get; init; } = new List<ReviewsDto>();
    
    public ReviewsInfo? ReviewsGeneral { get; init; }

}