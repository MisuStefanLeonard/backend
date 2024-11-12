namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ReviewsDto;

public class ReviewReceivedDto
{
    public string TextReview { get; set; } = null!;
    public int StarsReview { get; set; }
    public string CodProdus { get; set; } = null!;
    public int? IdSet { get; set; }

}