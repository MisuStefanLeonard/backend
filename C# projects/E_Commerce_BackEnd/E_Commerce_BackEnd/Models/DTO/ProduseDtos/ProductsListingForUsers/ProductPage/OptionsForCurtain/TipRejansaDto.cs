

using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;

public class TipRejansaDto 
{
    public int IdRejansa { get; init; }
    public string? NumeTipRejansa { get; init; }
    public Nume NumeTipRejansaDto { get; init; } = null!;
    public decimal PretTipRejansa { get; set; }
    public decimal IncretireRejansa { get; set; }
    public string? CaleRelativa { get; init; }
    public string? PresignedUrl { get; set; }
    public bool SePrindeCuInele { get; init; }
}