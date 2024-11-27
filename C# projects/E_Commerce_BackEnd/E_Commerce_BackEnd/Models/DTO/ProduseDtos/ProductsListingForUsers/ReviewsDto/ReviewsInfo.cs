namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;

public class ReviewsInfo
{
    public int TotalReviews { get; init; }
    public double AverageRating { get; init; }
    public int FiveStarsReviews { get; init; }
    public int FourStarsReviews { get; init; }
    public int ThreeStarsReviews { get; init; }
    public int TwoStarsReviews { get; init; }
    public int OneStarReviews { get; init; }

}