using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class ProductForSetDto
{
    public int IdProdusDto { get; init; } 
    public string CodProdusDto { get; init; } = null!;
    public string? NumeProdusDto { get; set; }
    public bool ActivInMagazinDto { get; set; }
    public decimal PretBazaDto { get; set; }
    public string TipProdusDto { get; set; } = null!;
    public ProductsVariatiesDto ProductOptions { get; } = new ProductsVariatiesDto();
    public IList<string> SelectedColors { get; } = new List<string>();
    public IList<string> SelectedDimensions { get; } = new List<string>();
}