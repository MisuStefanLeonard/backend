namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.MaterialeDtos;

public class MaterialeDto
{
    public string NumeMaterialDto { get; set; } = null!;
    public decimal PretMaterialDto { get; set; }
    public decimal PretMaterialRedusDto { get; set; }
    public bool MaterialActivInMagazinDto { get; set; }
    public string? CaleRelativa { get; set; } 
    public string PresignedUrl { get; init; } = null!;
}