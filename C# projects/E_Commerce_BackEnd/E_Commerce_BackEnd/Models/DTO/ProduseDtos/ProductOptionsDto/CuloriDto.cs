namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class CuloriDto
{
    public string NumeCuloareDto { get; set; } = null!;

    public string CodCuloareDto { get; set; } = null!;
    
    public bool JustAdded { get; set; } = false;
    public IList<ImagesDto>? ImaginiProdusDto { get; set; } = [];
}