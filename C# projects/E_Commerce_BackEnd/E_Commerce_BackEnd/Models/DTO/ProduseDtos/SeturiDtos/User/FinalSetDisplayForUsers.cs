namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.User;

public class FinalSetDisplayForUsers
{
    public IList<SetsListingForUsers> ShopSets { get; init; } = null!;
    public int TotalSetsListed { get; init; }

}