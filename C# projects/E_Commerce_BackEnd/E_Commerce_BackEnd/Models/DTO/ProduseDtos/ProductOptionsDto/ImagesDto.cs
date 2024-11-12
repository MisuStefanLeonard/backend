namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class ImagesDto
{
    public string CaleImagineDto { get; set; } = null!;
    public string FisierInBucketDto { get; set; } = null!;
    public string? PresignedUrl { get; set; }
    public bool JustAdded { get; set; } = false;
    public int IdProdusCuCuloareDto { get; set; }
}