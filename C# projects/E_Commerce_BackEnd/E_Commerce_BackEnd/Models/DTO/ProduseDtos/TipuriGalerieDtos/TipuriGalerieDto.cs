using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.TipuriGalerieDtos;

public class TipuriGalerieDto
{
    public string NumeTipGalerieDto { get; init; } = null!;
    public Nume NumeTipGalerieJsonDto { get; init; } = null!;
    public decimal PretTipGalerieDto { get; init; }
    public decimal IncretireDto { get; init; }
    public string? CaleRelativa { get; set; }
    public string PresignedUrl { get; set; } = null!;
    public bool SePrindeCuIneleDto { get; init; }
}