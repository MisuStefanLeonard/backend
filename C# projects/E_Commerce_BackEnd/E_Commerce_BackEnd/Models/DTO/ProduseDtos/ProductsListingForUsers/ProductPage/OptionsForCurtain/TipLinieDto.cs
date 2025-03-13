

using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;

public class TipLinieDto 
{
    public int IdTipLinie { get; init; }
    public string? NumeTipCusaturaColt { get; init; }
    public Nume NumeTipCusaturaColtJson { get; init; } = null!;
    public decimal PretTipCusaturaColt { get; set; } 
    public string? CaleRelativa { get; init; }
    public string? PresignedUrl { get; set; }
}