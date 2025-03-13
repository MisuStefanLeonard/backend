using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriLinieDtos;

public class TipuriLinieDto
{
    public string NumeTipLinieDto { get; set; } = null!;
    public Nume NumeTipLinieJsonDto { get; set; } = null!;
    public decimal PretPeTipLinieDto { get; set; }
    public string? CaleRelativa { get; set; }
    public string PresignedUrl { get; set; } = null!;
}