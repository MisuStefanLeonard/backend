using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ManopereDto.ManoperaForSet;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.ProductsListingForUsers.ProductPage.OptionsForCurtain;
using E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos.DtoForProductOptions;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class ProductForSetDto
{
    public int IdProdusDto { get; init; } 
    public string CodProdusDto { get; init; } = null!;
    public string? NumeProdusDto { get; init; }
    public bool ActivInMagazinDto { get; init; }
    public decimal PretBazaDto { get; init; }
    public string TipProdusDto { get; init; } = null!;
    public ProductsVariatiesDto ProductOptions { get; } = new ();
    public IList<string> SelectedColors { get; } = new List<string>();
    public IList<string> SelectedDimensions { get; } = new List<string>();
    public IList<string> SelectedManopere { get; } = new List<string>();

}