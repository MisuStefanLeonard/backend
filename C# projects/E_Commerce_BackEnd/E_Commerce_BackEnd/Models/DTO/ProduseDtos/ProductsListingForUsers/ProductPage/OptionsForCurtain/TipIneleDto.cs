

using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;

public class TipIneleDto 
{
    public int IdInelPrindere { get; init; }
    public string?  NumeTipInel{ get; init; } 
    public Culoare?  CuloareInelJsonDto { get; init; } 
    public string? CaleRelativa { get; init; }
    public string? PresignedUrl { get; set; }
}