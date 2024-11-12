

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;

public class TipRejansaDto 
{
    public string? NumeTipRejansa { get; init; } 
    public decimal PretTipRejansa { get; set; }
    public decimal IncretireRejansa { get; set; }
    public string? CaleRelativa { get; init; }
    public string? PresignedUrl { get; set; }
    public bool SePrindeCuInele { get; init; }
}