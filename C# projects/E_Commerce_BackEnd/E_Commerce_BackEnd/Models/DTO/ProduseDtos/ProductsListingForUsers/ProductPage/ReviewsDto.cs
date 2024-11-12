namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage;

public class ReviewsDto
{
    public int NumarSteleDto { get; init; }
    public string TextRecenzie { get; init; } = null!;
    public string? NumeClient { get; init; } 
    public string? PrenumeClient { get; init; }
    public string UsernameContClient { get; init; } = null!;
}