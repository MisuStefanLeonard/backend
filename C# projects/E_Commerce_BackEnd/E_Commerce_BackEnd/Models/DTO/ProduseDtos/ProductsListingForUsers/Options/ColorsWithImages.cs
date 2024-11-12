namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.Options;

public class ColorsWithImages
{
    public string NumeCuloareDto { get; set; } = null!;
    public IList<ImagesDtoForUsers>? ImaginiProdusDto { get; set; } = [];
}