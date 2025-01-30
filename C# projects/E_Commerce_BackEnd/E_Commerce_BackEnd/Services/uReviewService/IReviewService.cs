using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;

namespace E_Commerce_BackEnd.Services.uReviewService;

public interface IReviewService
{
    public Task<KeyValuePair<int, string>> PostReview(ReviewReceivedDto review, string token , string refreshToken);
   
}