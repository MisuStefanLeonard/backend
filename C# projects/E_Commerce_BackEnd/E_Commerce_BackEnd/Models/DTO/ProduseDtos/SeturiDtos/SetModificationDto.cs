using E_Commerce_BackEnd.Models.ProductRelatedModels.JSON_Models;

namespace E_Commerce_BackEnd.Models.DTO.ProduseDtos.SeturiDtos;

public class SetModificationDto
{
    public IList<ProductForSetDto> ProductsOnSet { get; init; } = new List<ProductForSetDto>();
    public string NumeSetDto { get; init; } = null!;
    public Nume NumeSetJsonDto { get; init; } = null!;
    public string DescriereSetDto { get; init; } = null!;
    public Descriere DescriereSetJsonDto { get; init; } = null!;
    public decimal PretSetDto { get; init; }
    public decimal PretRedusSetDto { get; init; }
}