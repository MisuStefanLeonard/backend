using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductOptionsDto;

public class TipuriProdusDto
{
    public string? CategorieDto { get; set; }
    public Categorie CategorieJsonDto { get; set; } = null!;
    public bool JustAdded { get; set; }
}