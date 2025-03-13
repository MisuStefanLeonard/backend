using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class CuloriDto
{
    public int? IdCuloare { get; init; }
    public string NumeCuloareDto { get; set; } = null!;
    public Culoare NumeCuloareJsonDto { get; set; } = null!;
    public string CodCuloareDto { get; set; } = null!;
    public bool JustAdded { get; set; } = false;
    public IList<ImagesDto>? ImaginiProdusDto { get; set; } = [];
}