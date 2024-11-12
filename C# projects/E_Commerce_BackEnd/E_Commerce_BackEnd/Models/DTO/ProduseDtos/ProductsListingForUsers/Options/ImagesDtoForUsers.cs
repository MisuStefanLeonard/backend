namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

public class ImagesDtoForUsers
{
    public string CaleImagineDto { get; set; } = null!;
    public string FisierInBucketDto { get; set; } = null!;
    public string? PresignedUrl { get; set; }
}