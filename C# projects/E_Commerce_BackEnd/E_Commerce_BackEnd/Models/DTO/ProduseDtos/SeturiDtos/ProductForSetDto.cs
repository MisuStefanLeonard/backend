
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;
using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class ProductForSetDto
{
    public int IdProdusDto { get; init; } 
    public string CodProdusDto { get; init; } = null!;
    public string? NumeProdusDto { get; init; }
    public Nume? NumeProdusJsonDto { get; set; }
    public bool ActivInMagazinDto { get; init; }
    public decimal PretBazaDto { get; init; }
    public string TipProdusDto { get; init; } = null!;
    public TipProdus? TipProdusJsonDto { get; set; }
    public ProductsVariatiesDto ProductOptions { get; } = new ();
    public IList<string> SelectedColors { get; } = new List<string>();
    public IList<string> SelectedDimensions { get; } = new List<string>();
    public IList<string> SelectedManopere { get; } = new List<string>();
    // public IList<Nume> SelectedManopereJson { get; } = new List<Nume>();


}