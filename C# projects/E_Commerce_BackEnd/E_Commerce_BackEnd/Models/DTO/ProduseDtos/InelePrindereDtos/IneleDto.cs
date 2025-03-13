using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.InelePrindereDtos;

public class IneleDto
{
    public string CuloareInelDto { get; set; } = null!;
    public Culoare CuloareInelJsonDto { get; set; } = null!;

    public string? CaleRelativa { get; set; } 
    public string PresignedUrl { get; init; } = null!;
}